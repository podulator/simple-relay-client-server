using System;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

namespace SpeedTester.Shared {

	public class Logger {
		private bool _verbose;

		public Logger (bool verbose) {
			this._verbose = verbose;
		}

		public bool Verbose {
			get { return this._verbose; }
			set { this._verbose = value; }
		}

		public void Log (string message, bool verbose = false) {
			if (verbose && !this._verbose) {
				return;
			}
			Console.WriteLine (message);
		}
	}

	public static class Helpers {

		public static string Version (Type assem) {
			Assembly thisAssem = assem.Assembly;
			AssemblyName thisAssemName = thisAssem.GetName ();
			Version ver = thisAssemName.Version;
			return ver.ToString ();
		}

		public static string SerializeToString (object o) {
			return SerializeToStream (o).ToString ();
		}

		public static MemoryStream SerializeToStream (object o) {
			using (MemoryStream stream = new MemoryStream ()) {
				IFormatter formatter = new BinaryFormatter ();
				formatter.Serialize (stream, o);
				return stream;
			}
		}

		public static object DeserializeFromStream (MemoryStream stream) {
			IFormatter formatter = new BinaryFormatter ();
			stream.Seek (0, SeekOrigin.Begin);
			object o = formatter.Deserialize (stream);
			return o;
		}

		public static void SendMessage (TcpClient client, Message message) {
			if (client == null || !client.Connected) return;
			MemoryStream msgStream = Helpers.SerializeToStream (message);
			Stream stream = client.GetStream ();
			stream.SendRawMessage (msgStream.ToArray ());
			stream.Flush ();
		}

		public static async Task<Message> ReceiveMessage (TcpClient client) {
			if (client == null || !client.Connected) return null;
			NetworkStream stream = client.GetStream ();
			byte[] raw = await stream.ReceiveRawMessage ();
			// by here we have data
			MemoryStream memStream = new MemoryStream (raw);
			BinaryFormatter formatter = new BinaryFormatter ();
			Message message = formatter.Deserialize (memStream) as Message;
			// stop the clock
			//message.finishHop ();
			return message;
		}
	}
}