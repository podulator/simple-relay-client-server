using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using SpeedTester.Shared;

namespace SpeedTester.Client {

    public class Client {
        private string _server;
        private string _name;
        private string _filter;
        private int _port;
        private IFH _firehose;
        private TcpClient _client;
        private Metrics _metrics;
        private bool _running = false;
        private Random _random;
        private Logger _logger;

        public Metrics Metrics { get { return this._metrics; } }

        public Client (string server, int port, string name, string filter, string firehoseEndpoint, bool verboseLogging) {
            this._metrics = new Metrics ();
            this._random = new Random ();
            this._logger = new Logger (verboseLogging);
            this._running = false;
            this._server = server;
            this._port = port;
            this._name = name;
            this._filter = filter;
            this._firehose = FireHoseFactory.GetFH (this._logger, firehoseEndpoint);
        }

        public async Task Receive () {
            this._logger.Log ("Receive called", true);
            Message message;
            bool jitterHandled = false;
            int jitterCounter = 0;
            using (var stream = this._client.GetStream ()) {
                this._logger.Log ("Will start producing stats when jitter has settled...", false);
                while (true) {
                    while ((message = await Helpers.ReceiveMessage (this._client)) != null) {
                        message.finishHop (this._name);
                        this._logger.Log ("Received message", true);
                        // skip metering the first few as the network settles down
                        if (jitterHandled) {
                            double elapsed = message.totalElapsedTime ().MS;
                            this._logger.Log (String.Format ("Total elapsed time : {0:F2} ms for {1} hops", elapsed, message.metrics.Count), true);
                            this._metrics.Register (message);
                            this._firehose.Send (message);
                        } else {
                            this._logger.Log (String.Format ("Jitter count: {0} / 10", jitterCounter), true);
                            jitterHandled = ++jitterCounter > 10;
                        }
                    }
                    if (!this._client.Connected) {
                        break;
                    }
                }
            }
            this._logger.Log ("Receive completed", true);
        }

        public void Send () {
            if (null != this._client && this._client.Connected) {
                this._logger.Log ("Send: client instantiated and connected", true);
                while (this._running && this._client.Connected) {
                    this._logger.Log ("Sending a new message", true);
                    int rndSleep = this._random.Next (1000) + 3500;
                    Thread.Sleep (rndSleep);
                    Message msg = new Message (this.MakePayload());
                    msg.addHop (this._name);
                    Helpers.SendMessage (this._client, msg);
                    this._logger.Log ("message sent", true);
                }
            } else {
                this._logger.Log (String.Format ("Send: client exists: {0}", null != this._client));
                this._logger.Log (String.Format ("Send: client connected: {0}", this._client.Connected));
            }
            this.Stats ();
        }

        private string MakePayload() {
            int index = this._random.Next(MessageHelper.NumSizes());
            int sizeToMake = MessageHelper.MessageSize(index);
            this._logger.Log(String.Format("Making a message with payload size: {0}", sizeToMake), true);
            sizeToMake -= this._filter.Length;
            string result = new string('=', sizeToMake);
            return this._filter + result;
        }

        private void Stats () {
            this._logger.Log ("<----------------------------------------------------->");
            this._logger.Log (String.Format ("Client connected: {0}", this._client.Connected));
            this._logger.Log (String.Format ("Thread running: {0}", this._running));
            if (this._metrics.Count > 0) {
                this._metrics.Show ();
            }
            this._logger.Log ("<----------------------------------------------------->");
        }

        public void Run () {

            this._logger.Log ("Running configured as :", false);
            this._logger.Log ("-----------------------", false);
            this._logger.Log (this.ToString (), false);
            this._logger.Log ("-----------------------", false);

            try {
                this._running = true;
                while (null == this._client || !this._client.Connected) {
                    try {
                        this._logger.Log ("Connecting to server...", false);
                        this._client = new TcpClient();
                        IAsyncResult result = this._client.BeginConnect(this._server, this._port, null, null);
                        bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(2));

                        if (!success) {
                            this._logger.Log ("Can't reach server, sleeping for 5 seconds...", false);
                            Thread.Sleep (5000);
                            if (!this._running) return;
                        }
                        this._logger.Log ("Connected to server", false);
                    } catch (Exception ex) {
                        this._logger.Log(ex.Message, false);
                        if (!this._running) return;
                    }
                }

                if (this._client.Connected) {

                    string ipAddress = this._client.Client.LocalEndPoint.ToString ();
                    this._logger.Log (String.Format ("Connected on local interface: {0}", ipAddress), true);

                    this._logger.Log ("Starting Send task...", true);
                    Task tSend = Task.Run (() => this.Send ());
                    this._logger.Log ("Starting Receive task...", true);
                    Task tReceive = Task.Run (() => this.Receive ());

                    while (this._running && this._client.Connected) {
                        Thread.Sleep (10000);
                        this.Stats ();
                    }
                    this._logger.Log ("Client shutting down...", true);
                } else {
                    this._logger.Log ("Couldn't connect to server, quitting...", false);
                }
            } catch (Exception e) {
                this._logger.Log ("Error connecting to server, quitting...", false);
                this._logger.Log (String.Format ("{0}", e.Message), false);
            }
            this._logger.Log ("Client exiting", false);
        }

        public override string ToString () {
            return String.Format ("server: {1}{0}port: {2}{0}name: {3}{0}filter: '{4}'{0}firehose: {5}{0}verbose: {6}",
                System.Environment.NewLine,
                this._server,
                this._port,
                this._name,
                this._filter, 
                this._firehose,
                this._logger.Verbose);
        }

        public void myHandler (object sender, ConsoleCancelEventArgs args) {
            this._logger.Log ("Manual request to shut down the client", false);
            // set this so we gracefully clean up and exit
            args.Cancel = true;
            this._running = false;
        }
    }
}
