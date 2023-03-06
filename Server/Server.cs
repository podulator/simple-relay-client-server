using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using SpeedTester.Shared;

namespace SpeedTester.Server {

    class Server {

        private int _port;
        private string _name;
        private string _filter;
        private IFH _firehose;
        private Logger _logger;
        private NetworkServer _server;

        public int Port { get { return this._port; } }
        public string Name { get { return this._name; } }
        public string Filter { get { return this._filter; } }
        public IFH Firehose { get { return this._firehose; } }
        public Logger Logger { get { return this._logger; } }

        public Server (int port, string name, string filter, string firehoseEndpoint, bool verbose) {
            this._port = port;
            this._name = name;
            this._filter = filter;
            this._logger = new Logger (verbose);
            this._firehose = FireHoseFactory.GetFH (this._logger, firehoseEndpoint);
        }

        public void Run () {

            this._logger.Log ("Running configured as :", false);
            this._logger.Log ("-----------------------", false);
            this._logger.Log (this.ToString (), false);
            this._logger.Log ("-----------------------", false);

            this._server = new NetworkServer (this);
            Task t = Task.Run (() => this._server.Run ());
            int tries = 0;
            while (tries++ < 10 && !this._server.isRunning) {
                this._logger.Log ("Waiting on listener...", true);
                System.Threading.Thread.Sleep (1000);
            }
            this._logger.Log ("Server started successfully", false);
            // main loop starts
            while (this._server.isRunning) {
                System.Threading.Thread.Sleep (1000);
                this._server.sweepConnections ();
            }
            this._logger.Log ("Server stopped, exiting", false);
        }

        public override string ToString () {
            return String.Format ("port: {0}{1}name: {2}{1}filter: '{3}'{1}firehose: {4}",
                this._port,
                System.Environment.NewLine,
                this._name, 
                this._filter, 
                this._firehose.Endpoint
            );
        }

        public void myHandler (object sender, ConsoleCancelEventArgs args) {
            this._logger.Log ("Manual request to shut down the server", false);
            // set this so we gracefuklly clean up and exit
            args.Cancel = true;
            this._server.Stop ();
        }
    }

    class NetworkClient {

        private NetworkServer _server;
        public TcpClient Client { set; get; }

        NetworkClient (NetworkServer server) {
            this._server = server;
        }
        ~NetworkClient () {
            if (null != this._server) {
                this._server.Logger.Log ("Network client destructor called", true);
            }
            this.Disconnect ();
        }

        public async System.Threading.Tasks.Task Run () {
            this._server.Logger.Log ("Client Run called", true);
            Message message;
            bool jitterHandled = false;
            int jitterCounter = 0;
            using (var stream = this.Client.GetStream ()) {
                while (true && this.Client.Connected) {
                    while ((message = await Helpers.ReceiveMessage (this.Client)) != null) {

                        this._server.Logger.Log ("Received message", true);
                        message.finishHop (this._server.Name);

                        // check if we drop it
                        if (this._server.Filter.Length > 0) {
                            if (!message.payload.Contains(this._server.Filter)) {
                                this._server.Logger.Log("Filtering out message", true);
                                continue;
                            }
                        }

                        // skip metering the first few as the network settles down
                        if (jitterHandled) {
                            this._server.Metrics.Register (message);
                            this._server.Firehose.Send (message);
                        } else {
                            if (++jitterCounter > 10) {
                                jitterHandled = true;
                            }
                        }

                        this._server.relayMessage (this, message);
                    }
                    if (!this._server.isRunning || !this.Client.Connected) {
                        break;
                    }
                }
            }
            this.Disconnect ();
            this._server.Logger.Log ("Client Run completed", true);
        }

        public void Disconnect () {
            try {
                this._server.Logger.Log ("Client disconnect called", true);
                if (this.Client.Connected) {
                    this._server.Logger.Log ("Closing client stream", true);
                    NetworkStream stream = Client.GetStream ();
                    if (null != stream) {
                        this._server.Logger.Log ("Closing client stream", true);
                        stream.Close ();
                    }
                    this._server.Logger.Log ("Closing client connection", true);
                    Client.Close ();
                }
                if (null != this._server) {
                    this._server.Logger.Log ("Removing from server client pool", true);
                    this._server.disconnectClient (this);
                }
                this._server.Logger.Log ("Client disconnect finished", true);
            } catch (Exception ex) {
                if (null != this._server) {
                    this._server.Logger.Log (ex.Message, false);
                }
            }

        }

        // we deliberately don't wait for the client.Run async
#pragma warning disable CS4014  
        public static NetworkClient CreateClient (NetworkServer server) {
            NetworkClient client = new NetworkClient (server);
            client.Client = server.Listener.AcceptTcpClient ();
            server.Logger.Log ("Connection accepted", true);
            client.Run ();
            return client;
        }
#pragma warning restore CS4014 
    }

    class NetworkServer {
        private static readonly int MAX_CLIENTS = 10;
        private Server _server = null;
        private bool _running = false;
        private bool _stopping = false;
        private List<NetworkClient> _clients = new List<NetworkClient> ();
        private List<NetworkClient> _disconnectingClients = new List<NetworkClient> ();
        private TcpListener _listener;

        private Metrics _metrics;
        public Metrics Metrics { get { return this._metrics; } }
        public TcpListener Listener { get { return this._listener; } }
        
        public Logger Logger { get { return this._server.Logger; } }
        public string Name { get { return this._server.Name; } }
        public string Filter { get { return this._server.Filter; } }
        public bool isRunning { get { return this._running; } }
        public IFH Firehose { get { return this._server.Firehose; } }
        
        public NetworkServer (Server server) {
            this._server = server;
            this._running = false;
            this._metrics = new Metrics ();
        }

        public void Run () {
            this._stopping = false;
            this._listener = new TcpListener (System.Net.IPAddress.Any, this._server.Port);
            this._listener.Start ();
            this._server.Logger.Log (String.Format ("Server Listening on: {0}", this._listener.LocalEndpoint), false);
            this._server.Logger.Log (String.Format ("Using name: {0}", this._server.Name), false);

            this._running = true;
            while (this._running) {

                // this blocks until a new client arrives
                if (!this._listener.Pending ()) {
                    System.Threading.Thread.Sleep (500);
                    continue;
                }
                if (!this._stopping) {
                    if (this._clients.Count >= NetworkServer.MAX_CLIENTS) {
                        this._server.Logger.Log (string.Format ("Client rejected, max connections reached: '{0}'", this._clients.Count), false);
                    } else {
                    NetworkClient client = NetworkClient.CreateClient (this);
                        this._server.Logger.Log (string.Format ("Client connected: '{0}'", client.GetHashCode ()), false);
                        this._clients.Add (client);
                        this._server.Logger.Log (string.Format ("Total clients connected: {0}", this._clients.Count), false);
                    }
                } else {
                    this._server.Logger.Log ("Not accepting new connection, server is stopping", false);
                }
            }
            this._server.Logger.Log ("Run is exiting", true);
        }

        public bool Stop () {
            this._server.Logger.Log ("Server Stop requested", true);
            this._stopping = true;
            this.disconnectAll ();
            this._listener.Stop ();
            this._running = false;
            return true;
        }

        private void disconnectAll () {
            this._server.Logger.Log (string.Format ("Disconnecting {0} clients", this._clients.Count), true);
            this._disconnectingClients.AddRange (this._clients);
            this._clients = new List<NetworkClient> ();
            this.gcConnections ();
            this.serverStats ();
        }

        public void sweepConnections () {
            int hanging = 0;
            foreach (NetworkClient client in this._clients) {
                if (!client.Client.Connected) {
                    this._disconnectingClients.Add (client);
                    ++hanging;
                }
            }
            if (hanging > 0) {
                this._server.Logger.Log (String.Format ("Sweeper wants to remove: {0} hanging connections", hanging), true);
                this.serverStats ();
                this.gcConnections ();
                this.serverStats ();
                this._server.Logger.Log ("Sweeper finished", true);
            }
        }
        private void gcConnections () {
            if (0 == this._disconnectingClients.Count) return;
            this._server.Logger.Log (String.Format (
                "Garbage collection running on {0} connections",
                this._disconnectingClients.Count), true);
            foreach (NetworkClient client in this._disconnectingClients) {
                this._clients.Remove (client);
            }
            this._disconnectingClients = new List<NetworkClient> ();
            this._server.Logger.Log ("Garbage collection finished", true);
        }

        public void serverStats () {
            this._server.Logger.Log (string.Format ("Total Clients connected: {0}", this._clients.Count), false);
            this._metrics.Show ();
        }

        public void disconnectClient (NetworkClient client) {
            this._server.Logger.Log (String.Format ("Disconnect client '{0}' requested", client.GetHashCode ()), true);
            this._disconnectingClients.Add (client);
            this._clients.Remove (client);
            this._server.Logger.Log (String.Format ("Client '{0}' disconnected", client.GetHashCode ()), true);
        }

        // Relays a message to all other connected clients
        public void relayMessage (NetworkClient fromClient, Message message) {
            int numInScope = this._clients.Count - 1;
            if (numInScope <= 0) {
                this._server.Logger.Log ("No connected clients to relay message to", true);
                return;
            }
            this._server.Logger.Log (string.Format ("relaying message to {0} clients", numInScope), true);
            foreach (NetworkClient client in this._clients) {
                // don't send to self
                if (fromClient != client) {
                    // only send to connected
                    if (client.Client.Connected) {
                        // and clone the original message, 
                        // or the hops will keep stacking up on each iteration
                        Message localMsg = message.Clone ();
                        localMsg.addHop (this._server.Name);
                        Helpers.SendMessage (client.Client, localMsg);
                    } else {
                        this._disconnectingClients.Add (client);
                    }
                }
            }
            // clean up any disconnected clients
            foreach (NetworkClient client in this._disconnectingClients) {
                this._clients.Remove (client);
            }
            // and reset the dirty pile
            this._disconnectingClients = new List<NetworkClient> ();
            this._server.Logger.Log (string.Format ("message relayed to {0} clients", numInScope), true);
            this.serverStats ();
        }
    }

}
