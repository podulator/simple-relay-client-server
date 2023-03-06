using CommandLine;
using SpeedTester.Shared;
using System;

namespace SpeedTester.Server {

    class CLOptions {
        [Option ('v', "verbose", Required = false, HelpText = "Set output to verbose.")]
        public bool Verbose { get; set; }

        [Option ('h', "help", Required = false, HelpText = "Show this help.")]
        public bool Help { get; set; }

        [Option ('p', "port", Required = false, HelpText = "Port the server is listening on. Default is 8080")]
        public int Port { get; set; }

        [Option ('n', "name", Required = false, HelpText = "Name for metrics aggregation. Default is 'server'.")]
        public string Name { get; set; }

        [Option ('f', "filter", Required = false, HelpText = "Payload contents filter to make sure the correct client is connected. Default is empty.")]
        public string Filter { get; set; }
        
        [Option ('k', "kinesis", Required = false, HelpText = "Kinesis Firehose name. Can be in the format region:name")]
        public string FireHose { get; set; }

    }

    class Program {

        static int Main (string[] args) {

            Console.Clear ();
            Console.WriteLine ("Test Server v.{0}", Helpers.Version (typeof (Program)));

            bool verbose = false;
            int port = Constants.DefaultPort;
            string name = Constants.EmptyString;
            string filter = Constants.EmptyString;
            string firehoseEndpoint = Constants.EmptyString;

            Parser.Default.ParseArguments<CLOptions> (args)
                .WithParsed<CLOptions> (o => {
                    verbose = o.Verbose;
                    port = o.Port;
                    name = o.Name ?? Constants.DefaultServerName;
                    filter = o.Filter ?? Constants.EmptyString;
                    firehoseEndpoint = o.FireHose ?? Constants.EmptyString;
                });

            Console.WriteLine ();
            Server server = new Server (port, name, filter, firehoseEndpoint, verbose);
            Console.CancelKeyPress += new ConsoleCancelEventHandler (server.myHandler);
            server.Run ();
            return 0;
        }

        public static void showHelp() {
            Console.WriteLine("Run as : {0} [port] [name] [filter] [firehose]", System.AppDomain.CurrentDomain.FriendlyName);
            Console.WriteLine("Port is optional, an integer, and defaults to 8080");
            Console.WriteLine("Name is optional, a string, and defaults to {0}", Constants.DefaultServerName);
            Console.WriteLine("Filter is optional, a string, and defaults to empty");
            Console.WriteLine("Kinesis FireHose is optional, a string, and defaults to empty");
        }

    }

}
