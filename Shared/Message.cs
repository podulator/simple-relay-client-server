using System;
using System.Collections.Generic;

namespace SpeedTester.Shared {

    public static class MessageHelper {
        public enum MessageLengthBytes {
            Small = 16,
            Medium = 512,
            Large = 1024,
            XLarge = 2048
        }
        public static int MessageSize(int index) {
            switch (index) {
                case 0:
                    return (int)MessageLengthBytes.Small;
                case 1:
                    return (int)MessageLengthBytes.Medium;
                case 2:
                    return (int)MessageLengthBytes.Large;
                case 3:
                    return (int)MessageLengthBytes.XLarge;
                default:
                    return (int)MessageLengthBytes.Small;
            };
        }
        public static int NumSizes() {
            return sizeof(MessageLengthBytes);
        }
    }
    public class Metrics {

        public struct ValuePair {
            public ulong InstanceCount;
            public double TotalValue;
        }

        public int Count { get { return this._collection.Count; } }

        private Dictionary<string, ValuePair> _collection;

        public Metrics () {
            this._collection = new Dictionary<string, ValuePair> ();
        }

        public void Register (Message message) {
            // add the individual hops
            foreach (Hop hop in message.metrics) {
                string key = hop.Elapsed ().Key;
                if (!this._collection.ContainsKey (key)) {
                    this._collection.Add (key, new ValuePair {
                        InstanceCount = 1,
                            TotalValue = hop.Elapsed ().MS
                    });
                } else {
                    ValuePair vp = this._collection[key];
                    vp.InstanceCount += 1;
                    vp.TotalValue += hop.Elapsed ().MS;
                    this._collection[key] = vp;
                }

            }
            // plus a combined one
            ElapsedTime totalElapsed = message.totalElapsedTime ();
            string combinedKey = totalElapsed.Key;
            if (!this._collection.ContainsKey (combinedKey)) {
                this._collection.Add (combinedKey, new ValuePair {
                    InstanceCount = 1,
                        TotalValue = totalElapsed.MS
                });
            } else {
                ValuePair vp = this._collection[combinedKey];
                vp.InstanceCount++;
                vp.TotalValue += totalElapsed.MS;
                this._collection[combinedKey] = vp;
            }
        }

        public void Show () {
            Console.Write (this.ToString ());
        }

        public override string ToString () {
            string result = "";
            foreach (KeyValuePair<string, ValuePair> iter in this._collection) {
                double average = iter.Value.TotalValue / iter.Value.InstanceCount;
                result += String.Format ("Group '{0}' - {1:F2} ms average time over {2} data points{3}",
                    iter.Key,
                    average,
                    iter.Value.InstanceCount,
                    Environment.NewLine);
            }
            return result;
        }
    }

    [Serializable ()]
    public struct ElapsedTime {
        public string Key;
        public double MS;
    };

    [Serializable ()]
    public class Hop {

        [Serializable ()]
        public struct DataPoint {
            public Nullable<DateTime> Timestamp;
            public string Group;
        };

        public const string DefaultGroup = "all";

        private DataPoint _sent;
        private DataPoint _received;

        public Hop (string group = DefaultGroup) {
            this._sent.Group = group;
            this._sent.Timestamp = DateTime.UtcNow;
        }

        public Hop (Hop copy) {
            this._sent = copy.Sent;
            this._received = copy.Received;
        }

        public DataPoint Sent {
            get { return this._sent; }
            set { this._sent = value; }
        }

        public DataPoint Received {
            get { return this._received; }
            set { this._received = value; }
        }

        public void Finalise (string group = DefaultGroup) {
            this._received.Timestamp = DateTime.UtcNow;
            this._received.Group = group;
        }
        public ElapsedTime Elapsed () {
            ElapsedTime result = new ElapsedTime ();

            if (null != this.Received.Timestamp) {
                result.Key = this._sent.Group + Constants.HopDelimiter + this.Received.Group;
                TimeSpan? elapsed = (this.Received.Timestamp - this.Sent.Timestamp);
                result.MS = elapsed.Value.TotalMilliseconds;
            } else {
                result.Key = this._sent.Group;
                result.MS = 0d;
            }

            return result;
        }

        public override string ToString() {
            return base.ToString();
        }

    }

    [Serializable ()]
    public class Message {

        public string payload { get; private set; }
        public int payloadLength { get; private set; }

        public List<Hop> metrics { get; private set; }

        public Message (string message) {
            this.payload = message;
            this.payloadLength = message.Length;
            this.metrics = new List<Hop> ();
        }

        public void addHop (string group = Hop.DefaultGroup) {
            this.metrics.Add (new Hop (group));
        }
        public void finishHop (string group = Hop.DefaultGroup) {
            this.metrics.FindLast (item => item.Received.Timestamp == null).Finalise (group);
        }
        public ElapsedTime totalElapsedTime () {
            ElapsedTime result = new ElapsedTime ();
            List<string> keyParts = new List<string> ();
            foreach (Hop hop in this.metrics) {
                result.MS += hop.Elapsed ().MS;
                keyParts.Add (hop.Elapsed ().Key);
            }
            result.Key = String.Join (Constants.SegmentDelimiter, keyParts);
            return result;
        }

        public int numHops () {
            return this.metrics.Count;
        }

        public Message Clone () {
            Message result = new Message (this.payload);
            this.metrics.ForEach ((hop) => {
                result.metrics.Add (new Hop (hop));
            });
            return result;
        }

        public override string ToString () {
            return this.payload;
        }

        public List<string> ToJsonMetric() {
            List<string> results = new List<string>();
            foreach (Hop hop in this.metrics) {
                ElapsedTime elapsed = hop.Elapsed();
                string json = String.Format("{{ \"key\":\"{0}\", \"ms\":{1}, \"length\":{2}, \"received\":\"{3}\" }}", 
                    elapsed.Key, 
                    elapsed.MS, 
                    this.payloadLength, 
                    hop.Received);
                results.Add(json);
            }
            return results;
        }
    }
}
