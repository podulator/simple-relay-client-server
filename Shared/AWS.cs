using Amazon;
using Amazon.KinesisFirehose;
using Amazon.KinesisFirehose.Model;
using SpeedTester.Shared;
using System;
using System.Collections.Generic;

namespace SpeedTester.Shared {

    public static class FireHoseFactory {
        public static IFH GetFH (Logger logger, string endpoint = Constants.EmptyString) {
            if (string.Empty.CompareTo (endpoint) == 0) {
                return new DummyFH (logger, endpoint);
            }
            return new KFH (logger, endpoint);
        }
    }

    public abstract class IFH {
        protected Logger _logger;
        protected string _endpoint;
        protected IFH (Logger logger, string endpoint) {
            this._logger = logger;
            this._endpoint = endpoint;
        }
        public virtual void Send (Message msg) { return; }
        public string Endpoint { get { return this._endpoint; } }
    }

    public class DummyFH : IFH {
        public DummyFH (Logger logger, string endpoint) : base (logger, endpoint) {
            this._logger.Log ("Creating a DummyFH Instance", true);
        }

        public override void Send (Message msg) {
            this._logger.Log ("DummyFH::Send", true);
            this._logger.Log (msg.ToString (), true);
            return;
        }
    }

    public class KFH : IFH {

        AmazonKinesisFirehoseClient _client;

        public KFH (Logger logger, string endpoint) : base (logger, endpoint) {
            if (endpoint.Contains(":")) {
                string[] parts = endpoint.Split(':');
                string region = parts[0];
                endpoint = parts[1];
                RegionEndpoint regionEndpoint = RegionEndpoint.GetBySystemName(region);
                this._logger.Log (String.Format ("Creating a KFH Instance for: '{0}' in region '{1}'", endpoint, regionEndpoint.DisplayName), true);
                this._client = new AmazonKinesisFirehoseClient (regionEndpoint);
            } else {
                this._logger.Log (String.Format ("Creating a KFH Instance for: '{0}'", endpoint), true);
                this._client = new AmazonKinesisFirehoseClient ();
            }
            this._endpoint = endpoint;
        }

        public override void Send (Message msg) {
            this._logger.Log ("KFH::Send", true);
            List<Record> records = new List<Record> ();
            foreach (string json in msg.ToJsonMetric()) {
                this._logger.Log (String.Format ("KFH Sending: '{0}'", json), true);
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes (json);
                Record record = new Record () {
                    Data = new System.IO.MemoryStream (bytes)
                };
                records.Add (record);
            }
            this._client.PutRecordBatchAsync (this._endpoint, records);
            return;
        }
    }
}
