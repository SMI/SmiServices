
using Equ;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Smi.Common.Messages
{
    public class MessageHeader : MemberwiseEquatable<MessageHeader>, IMessageHeader
    {
        public Guid MessageGuid { get; init; }

        public int ProducerProcessID { get; init; }

        public string ProducerExecutableName { get; init; }

        public long OriginalPublishTimestamp { get; init; }

        public Guid[] Parents { get; init; }
        public const string Splitter = "->";

        private static readonly int _producerProcessID;
        private static readonly string _producerExecutableName;


        static MessageHeader()
        {
            _producerExecutableName = Assembly.GetEntryAssembly()?.GetName().Name ?? "dotnet";
            _producerProcessID = Process.GetCurrentProcess().Id;
        }

        [JsonConstructor]
        public MessageHeader()
            : this(parent: default) { }

        /// <summary>
        /// Declares that your process is about to send a message.  Optionally as a result of processing another message (<paramref name="parent"/>).
        /// </summary>
        /// <param name="parent">The triggering message that caused you to want to send this message</param>
        public MessageHeader(IMessageHeader? parent = null)
        {
            ProducerProcessID = _producerProcessID;
            ProducerExecutableName = _producerExecutableName;
            MessageGuid = Guid.NewGuid();

            if (parent == null)
            {
                Parents = Array.Empty<Guid>();
                OriginalPublishTimestamp = UnixTimeNow();
            }
            else
            {
                var p = new List<Guid>(parent.Parents) { parent.MessageGuid };
                Parents = p.ToArray();
                OriginalPublishTimestamp = parent.OriginalPublishTimestamp;
            }
        }

        /// <summary>
        /// Creates a <see cref="MessageHeader"/> out of a (byte-encoded) header field set from RabbitMQ
        /// </summary>
        /// <param name="encodedHeaders"></param>
        /// <param name="enc"></param>
        public MessageHeader(IDictionary<string, object> encodedHeaders, Encoding enc)
        {
            MessageGuid = GetGuidArrayFromEncodedHeader(encodedHeaders["MessageGuid"], enc).Single();
            ProducerProcessID = (int)encodedHeaders["ProducerProcessID"];
            ProducerExecutableName = enc.GetString((byte[])encodedHeaders["ProducerExecutableName"]);
            Parents = GetGuidArrayFromEncodedHeader(encodedHeaders["Parents"], enc);
            OriginalPublishTimestamp = Convert.ToInt64(encodedHeaders["OriginalPublishTimestamp"]); // XXX error casting from Int32 to Int64 using (long)
        }

        /// <summary>
        /// Creates a <see cref="MessageHeader"/> out of a header field that has been serialized
        /// </summary>
        /// <param name="headers"></param>
        public MessageHeader(IDictionary<string, object> headers)
        {
            MessageGuid = Guid.Parse((string)headers["MessageGuid"]);
            ProducerProcessID = (int)headers["ProducerProcessID"];
            ProducerExecutableName = (string)headers["ProducerExecutableName"];
            OriginalPublishTimestamp = (long)headers["OriginalPublishTimestamp"];
            Parents = GetGuidArray((string)headers["Parents"]);
        }


        /// <summary>
        /// Populates RabbitMQ header properties with the current MessageHeader
        /// </summary>
        /// <param name="headers"></param>
        public void Populate(IDictionary<string, object> headers)
        {
            headers.Add("MessageGuid", MessageGuid.ToString());
            headers.Add("ProducerProcessID", ProducerProcessID);
            headers.Add("ProducerExecutableName", ProducerExecutableName);
            headers.Add("OriginalPublishTimestamp", OriginalPublishTimestamp);
            headers.Add("Parents", string.Join(Splitter, Parents));
        }

        public bool IsDescendantOf(IMessageHeader other)
        {
            return Parents != null && Parents.Contains(other.MessageGuid);
        }

        public void Log(ILogger logger, LogLevel level, string message, Exception? ex = null)
        {
            //TODO This is massively over-logging - ProducerProcessID, ProducerExecutableName, OriginalPublishTimestamp are found in the logs anyway
            var theEvent = new LogEventInfo(level, logger.Name, message);
            theEvent.Properties["MessageGuid"] = MessageGuid.ToString();
            theEvent.Properties["ProducerProcessID"] = ProducerProcessID;
            theEvent.Properties["ProducerExecutableName"] = ProducerExecutableName;
            theEvent.Properties["OriginalPublishTimestamp"] = OriginalPublishTimestamp;
            theEvent.Properties["Parents"] = string.Join(Splitter, Parents);
            theEvent.Exception = ex;

            logger.Log(theEvent);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("MessageGuid: " + MessageGuid);
            sb.Append(", ProducerProcessID: " + ProducerProcessID);
            sb.Append(", ProducerExecutableName: " + ProducerExecutableName);
            sb.Append(", OriginalPublishTimestamp:" + OriginalPublishTimestamp);
            sb.Append(", Parents: [" + string.Join(Splitter, Parents) + "]");
            return sb.ToString();
        }

        // TODO(rkm 2020-03-08) Can't we just use the DateTime.UnixEpoch value here?
        public static long UnixTimeNow() => UnixTime(DateTime.UtcNow);
        public static long UnixTime(DateTime dateTime) => (long)(dateTime - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
        public static DateTime UnixTimeToDateTime(long unixTime) => new DateTime(1970, 1, 1, 0, 0, 0) + TimeSpan.FromSeconds(unixTime);

        public static Guid[] GetGuidArray(string str)
        {
            string[] strings = str.Split(new[] { Splitter }, StringSplitOptions.RemoveEmptyEntries);
            return strings.Select(Guid.Parse).ToArray();
        }

        private static Guid[] GetGuidArrayFromEncodedHeader(object o, Encoding enc)
        {
            return GetGuidArray(enc.GetString((byte[])o));
        }
    }
}
