using CommandLine;
using SpeedTester.Shared;
using System;

namespace SpeedTester.Client {

    class CLOptions {
        [Option ('v', "verbose", Required = false, HelpText = "Set output to verbose.")]
        public bool Verbose { get; set; }

        [Option ('h', "help", Required = false, HelpText = "Show this help.")]
        public bool Help { get; set; }

        [Option ('s', "server", Required = true, HelpText = "Server ip or DNS name.")]
        public string Server { get; set; }

        [Option ('p', "port", Required = false, HelpText = "Port the server is listening on. Default is 8080")]
        public int Port { get; set; }

        [Option ('n', "name", Required = false, HelpText = "Name for metrics aggregation. Default is 'client'.")]
        public string Name { get; set; }

        [Option ('k', "kinesis", Required = false, HelpText = "Kinesis Firehose name. Can be in the format region:name")]
        public string FireHose { get; set; }

        [Option ('f', "filter", Required = false, HelpText = "Payload contents filter to make sure the correct client is connected. Default is empty.")]
        public string Filter { get; set; }
    }

    class Program {

        static int Main (string[] args) {

            Console.Clear ();
            Console.WriteLine ("Test Client v.{0}", Helpers.Version (typeof (Program)));

            bool verbose = false;
            int port = Constants.DefaultPort;
            string name = Constants.EmptyString;
            string filter = Constants.EmptyString;
            string firehoseEndpoint = Constants.EmptyString;
            string server = Constants.EmptyString;

            Parser.Default.ParseArguments<CLOptions> (args)
                .WithParsed<CLOptions> (o => {
                    verbose = o.Verbose;
                    port = o.Port;
                    server = o.Server ?? Constants.DefaultServer;
                    name = o.Name ?? Constants.DefaultClientName;
                    filter = o.Filter ?? Constants.EmptyString;
                    firehoseEndpoint = o.FireHose ?? Constants.EmptyString;
                });

            Console.WriteLine ();
            Client app = new Client (server, port, name, filter, firehoseEndpoint, verbose);
            Console.CancelKeyPress += new ConsoleCancelEventHandler (app.myHandler);
            app.Run ();
            return 0;
        }
    }
}
