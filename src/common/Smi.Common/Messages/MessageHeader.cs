
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using NLog;

namespace Smi.Common.Messages
{
    public class MessageHeader : IMessageHeader, IEquatable<MessageHeader>
    {
        // FIXME: Pull out header property strings into const. variables

        public Guid MessageGuid { get; set; }

        public int ProducerProcessID { get; set; }

        public string ProducerExecutableName { get; set; }

        public long OriginalPublishTimestamp { get; set; }

        public Guid[] Parents { get; set; }
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
            : this(default(MessageHeader)) { }

        /// <summary>
        /// Declares that your process is about to send a message.  Optionally as a result of processing another message (<see cref="parent"/>).
        /// </summary>
        /// <param name="parent">The triggering message that caused you to want to send this message</param>
        public MessageHeader(IMessageHeader parent = null)
        {
            ProducerProcessID = _producerProcessID;
            ProducerExecutableName = _producerExecutableName;
            MessageGuid = Guid.NewGuid();

            if (parent == null)
            {
                Parents = new Guid[0];
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

        public void Log(ILogger logger, LogLevel level, string message, Exception ex = null)
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

        public static long UnixTimeNow()
        {
            return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
        }

        public static Guid[] GetGuidArray(string str)
        {
            string[] strings = str.Split(new[] { Splitter }, StringSplitOptions.RemoveEmptyEntries);
            return strings.Select(Guid.Parse).ToArray();
        }

        private static Guid[] GetGuidArrayFromEncodedHeader(object o, Encoding enc)
        {
            return GetGuidArray(enc.GetString((byte[])o));
        }

        #region Equality Members

        /// <inheritdoc />
        public bool Equals(MessageHeader other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return MessageGuid.Equals(other.MessageGuid)
                   && ProducerProcessID == other.ProducerProcessID
                   && string.Equals(ProducerExecutableName, other.ProducerExecutableName)
                   && OriginalPublishTimestamp == other.OriginalPublishTimestamp
                   && Parents.SequenceEqual(other.Parents);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MessageHeader)obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = MessageGuid.GetHashCode();
                hashCode = (hashCode * 397) ^ ProducerProcessID;
                hashCode = (hashCode * 397) ^ (ProducerExecutableName != null ? ProducerExecutableName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ OriginalPublishTimestamp.GetHashCode();
                hashCode = (hashCode * 397) ^ (Parents != null ? Parents.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(MessageHeader left, MessageHeader right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(MessageHeader left, MessageHeader right)
        {
            return !Equals(left, right);
        }

        #endregion
    }
}
