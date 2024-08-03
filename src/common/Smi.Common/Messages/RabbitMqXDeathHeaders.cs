#nullable disable

using Equ;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Smi.Common.Messages
{
    public class RabbitMqXDeathHeaders : MemberwiseEquatable<RabbitMqXDeathHeaders>
    {
        public List<RabbitMqXDeath> XDeaths { get; set; }

        public string XFirstDeathExchange { get; set; }

        public string XFirstDeathQueue { get; set; }

        public string XFirstDeathReason { get; set; }

        public const string XDeathKey = "x-death";
        public const string XFirstDeathExchangeKey = "x-first-death-exchange";
        public const string XFirstDeathQueueKey = "x-first-death-queue";
        public const string XFristDeathReasonKey = "x-first-death-reason";


        private static readonly List<string> _requiredKeys;


        /// <summary>
        /// Static constructor
        /// </summary>
        static RabbitMqXDeathHeaders()
        {
            _requiredKeys = new List<string>
            {
                XDeathKey,
                XFirstDeathExchangeKey,
                XFirstDeathQueueKey,
                XFristDeathReasonKey
            };
        }

        public RabbitMqXDeathHeaders() { }

        /// <summary>
        /// Creates a <see cref="RabbitMqXDeathHeaders"/> out of a (byte-encoded) header field set from RabbitMQ
        /// </summary>
        /// <param name="encodedHeaders"></param>
        /// <param name="enc"></param>
        public RabbitMqXDeathHeaders(IDictionary<string, object> encodedHeaders, Encoding enc)
        {
            if (!(encodedHeaders.Any() && _requiredKeys.All(encodedHeaders.ContainsKey)))
                throw new ArgumentException("xDeathEntry");

            XDeaths = new List<RabbitMqXDeath>();

            foreach (object xDeathEntry in (List<object>)encodedHeaders[XDeathKey])
                XDeaths.Add(new RabbitMqXDeath((Dictionary<string, object>)xDeathEntry, enc));

            XFirstDeathExchange = enc.GetString((byte[])encodedHeaders[XFirstDeathExchangeKey]);
            XFirstDeathQueue = enc.GetString((byte[])encodedHeaders[XFirstDeathQueueKey]);
            XFirstDeathReason = enc.GetString((byte[])encodedHeaders[XFristDeathReasonKey]);
        }


        public void Populate(IDictionary<string, object> headers)
        {
            var xDeaths = new List<object>();
            foreach (RabbitMqXDeath item in XDeaths)
            {
                xDeaths.Add(new Dictionary<string, object>
                {
                    { RabbitMqXDeath.CountKey, item.Count },
                    { RabbitMqXDeath.ExchangeKey, item.Exchange },
                    { RabbitMqXDeath.QueueKey, item.Queue },
                    { RabbitMqXDeath.ReasonKey, item.Reason },
                    { RabbitMqXDeath.RoutingKeysKey, item.RoutingKeys },
                    { RabbitMqXDeath.TimeKey, new AmqpTimestamp(item.Time) }
                });
            }

            headers.Add(XDeathKey, xDeaths);
            headers.Add(XFirstDeathExchangeKey, XFirstDeathExchange);
            headers.Add(XFirstDeathQueueKey, XFirstDeathQueue);
            headers.Add(XFristDeathReasonKey, XFirstDeathReason);
        }

        public static void CopyHeaders(IDictionary<string, object> from, IDictionary<string, object> to)
        {
            // Ensure that /from/ contains all the required headers, and /to/ contains none of them

            if (from == null || !(from.Any() && _requiredKeys.All(from.ContainsKey)))
                throw new ArgumentException("from");

            if (to == null || _requiredKeys.Any(to.ContainsKey))
                throw new ArgumentException("to");

            foreach (string key in _requiredKeys)
                to.Add(key, from[key]);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("XFirstDeathExchange: " + XFirstDeathExchange);
            sb.Append(", XFirstDeathQueue: " + XFirstDeathQueue);
            sb.Append(", XFirstDeathReason: " + XFirstDeathReason);
            sb.Append(", XDeaths: {" + string.Join(", ", XDeaths) + "}");
            return sb.ToString();
        }
    }

    public class RabbitMqXDeath : MemberwiseEquatable<RabbitMqXDeath>
    {
        public const string CountKey = "count";
        public const string ExchangeKey = "exchange";
        public const string QueueKey = "queue";
        public const string ReasonKey = "reason";
        public const string RoutingKeysKey = "routing-keys";
        public const string TimeKey = "time";
        private static readonly List<string> _requiredKeys;

        public long Count { get; set; }

        public string Exchange { get; set; }

        public string Queue { get; set; }

        public string Reason { get; set; }

        public List<string> RoutingKeys { get; set; }

        public long Time { get; set; }


        static RabbitMqXDeath()
        {
            _requiredKeys = new List<string>
            {
                CountKey,
                ExchangeKey,
                QueueKey,
                ReasonKey,
                RoutingKeysKey,
                TimeKey,
            };
        }

        public RabbitMqXDeath() { }

        public RabbitMqXDeath(IDictionary<string, object> xDeathEntry, Encoding enc)
        {
            if (xDeathEntry == null ||
                !(xDeathEntry.Any() && xDeathEntry.All(k => _requiredKeys.Contains(k.Key))))
                throw new ArgumentException("xDeathEntry");

            Count = (long)xDeathEntry[CountKey];
            Exchange = enc.GetString((byte[])xDeathEntry[ExchangeKey]);
            Queue = enc.GetString((byte[])xDeathEntry[QueueKey]);
            Reason = enc.GetString((byte[])xDeathEntry[ReasonKey]);
            RoutingKeys = ((List<object>)xDeathEntry[RoutingKeysKey]).Select(x => enc.GetString((byte[])x)).ToList();
            Time = ((AmqpTimestamp)xDeathEntry[TimeKey]).UnixTime;
        }


        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("Count: " + Count);
            sb.Append(", Exchange: " + Exchange);
            sb.Append(", Queue: " + Queue);
            sb.Append(", Reason: " + Reason);
            sb.Append(", RoutingKeys: {" + string.Join(", ", RoutingKeys) + "}");
            return sb.ToString();
        }
    }
}
