
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using SmiServices.Common.Events;
using SmiServices.Common.Messages;
using SmiServices.Common.Options;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SmiServices.Common.Messaging
{
    public class ControlMessageConsumer : Consumer<IMessage>
    {
        public readonly ConsumerOptions ControlConsumerOptions = new()
        {
            QoSPrefetchCount = 1,
            AutoAck = true
        };

        public event StopEventHandler StopHost;
        public event ControlEventHandler? ControlEvent;


        private readonly string _processName;
        private readonly string _processId;

        private readonly IChannel _channel;

        private const string ControlQueueBindingKey = "smi.control.all.*";


        public ControlMessageConsumer(
            RabbitOptions rabbitOptions,
            string processName,
            int processId,
            string controlExchangeName,
            Action<string> stopEvent)
        {
            ArgumentNullException.ThrowIfNull(processName);
            ArgumentNullException.ThrowIfNull(controlExchangeName);
            ArgumentNullException.ThrowIfNull(stopEvent);

            if (rabbitOptions.RabbitMqVirtualHost is null || rabbitOptions.RabbitMqUserName is null || rabbitOptions.RabbitMqPassword is null)
                throw new InvalidOperationException("RabbitOptions must have all fields set");

            _processName = processName.ToLower();
            _processId = processId.ToString();

            ControlConsumerOptions.QueueName = $"Control.{_processName}{_processId}";

            _channel = new ConnectionFactory
            {
                HostName = rabbitOptions.RabbitMqHostName,
                VirtualHost = rabbitOptions.RabbitMqVirtualHost,
                Port = rabbitOptions.RabbitMqHostPort,
                UserName = rabbitOptions.RabbitMqUserName,
                Password = rabbitOptions.RabbitMqPassword
            }.CreateConnectionAsync(CancellationToken.None).Result.CreateChannelAsync(null, CancellationToken.None).Result;

            SetupControlQueueForHost(controlExchangeName).Wait();

            StopHost += () => stopEvent("Control message stop");
        }

        /// <summary>
        /// Recreate ProcessMessage to specifically handle control messages which won't have headers,
        /// and shouldn't be included in any Ack/Nack counts
        /// </summary>
        /// <param name="e"></param>
        public override void ProcessMessage(BasicDeliverEventArgs e)
        {
            try
            {
                // For now we only deal with the simple case of "smi.control.<who>.<what>". Can expand later on depending on our needs
                // Queues will be deleted when the connection is closed so don't need to worry about messages being leftover

                Logger.Info("Control message received with routing key: " + e.RoutingKey);

                string[] split = e.RoutingKey.ToLower().Split('.');
                string? body = GetBodyFromArgs(e);

                if (split.Length < 4)
                {
                    Logger.Debug("Control command shorter than the minimum format");
                    return;
                }

                // Who, what
                string actor = string.Join(".", split.Skip(2).Take(split.Length - 3));
                string action = split[^1];

                // If action contains a numeric and it's not our PID, then ignore
                if (action.Any(char.IsDigit) && !action.EndsWith(_processId, StringComparison.Ordinal))
                    return;

                // Ignore any messages not meant for us
                if (!actor.Equals("all") && !actor.Equals(_processName))
                {
                    Logger.Debug("Control command did not match this service");
                    return;
                }

                // Handle any general actions - just stop and ping for now

                if (action.StartsWith("stop", StringComparison.Ordinal))
                {
                    Logger.Info("Stop request received, raising StopHost event");
                    Task.Run(() => StopHost.Invoke());

                    return;
                }

                if (action.StartsWith("ping", StringComparison.Ordinal))
                {
                    Logger.Info("Pong!");
                    return;
                }

                // Don't pass any unhandled broadcast (to "all") messages down to the hosts
                if (actor.Equals("all"))
                    return;

                // Else raise the event if any hosts have specific control needs
                if (ControlEvent != null)
                {
                    Logger.Debug("Control message not handled, raising registered ControlEvent(s)");
                    ControlEvent(Regex.Replace(action, @"[\d]", ""), body);

                    return;
                }

                // Else we should ignore it?
                Logger.Warn("Unhandled control message with routing key: " + e.RoutingKey);
            }
            catch (Exception exception)
            {
                Fatal("ProcessMessageImpl threw unhandled exception", exception);
            }
        }

        /// <summary>
        /// Ensures the control queue is cleaned up on exit. Should have been deleted already, but this ensures it
        /// </summary>
        public override void Shutdown()
        {
            Logger.Debug($"Deleting control queue: {ControlConsumerOptions.QueueName}");
            if (ControlConsumerOptions.QueueName != null)
                _channel.QueueDeleteAsync(ControlConsumerOptions.QueueName).Wait();
        }

        // NOTE(rkm 2020-05-12) Not used in this implementation
        protected override void ProcessMessageImpl(IMessageHeader header, IMessage message, ulong tag) => throw new NotImplementedException("ControlMessageConsumer does not implement ProcessMessageImpl");

        // NOTE(rkm 2020-05-12) Control messages are automatically acknowledged, so nothing to do here
        protected override void ErrorAndNack(IMessageHeader header, ulong tag, string message, Exception exc) => throw new NotImplementedException($"ErrorAndNack called for control message {tag} ({exc})");

        /// <summary>
        /// Creates a one-time connection to set up the required control queue and bindings on the RabbitMQ server.
        /// The connection is disposed and StartConsumer(...) can then be called on the parent MessageBroker with ControlConsumerOptions
        /// </summary>
        /// <param name="controlExchangeName"></param>
        private async Task SetupControlQueueForHost(string controlExchangeName)
        {
            try
            {
                await _channel.ExchangeDeclarePassiveAsync(controlExchangeName, CancellationToken.None);
            }
            catch (OperationInterruptedException e)
            {
                throw new ApplicationException($"The given control exchange was not found on the server: \"{controlExchangeName}\"", e);
            }

            Logger.Debug($"Creating control queue {ControlConsumerOptions.QueueName}");

            // Declare our queue with:
            // durable = false (queue will not persist over restarts of the RabbitMq server)
            // exclusive = false (queue won't be deleted when THIS connection closes)
            // autoDelete = true (queue will be deleted after a consumer connects and then disconnects)
            await _channel.QueueDeclareAsync(
                ControlConsumerOptions.QueueName ??
                throw new InvalidOperationException(nameof(ControlConsumerOptions.QueueName)), durable: false,
                exclusive: false, autoDelete: true, cancellationToken: CancellationToken.None);

            // Binding for any control requests, i.e. "stop"
            Logger.Debug($"Creating binding {controlExchangeName}->{ControlConsumerOptions.QueueName} with key {ControlQueueBindingKey}");
            await _channel.QueueBindAsync(ControlConsumerOptions.QueueName, controlExchangeName, ControlQueueBindingKey);

            // Specific microservice binding key, ignoring the id at the end of the process name
            var bindingKey = $"smi.control.{_processName}.*";

            Logger.Debug($"Creating binding {controlExchangeName}->{ControlConsumerOptions.QueueName} with key {bindingKey}");
            await _channel.QueueBindAsync(ControlConsumerOptions.QueueName, controlExchangeName, bindingKey);
        }

        private static string? GetBodyFromArgs(BasicDeliverEventArgs e)
        {
            if (e.Body.Length == 0)
                return null;

            Encoding? enc = null;

            if (!string.IsNullOrWhiteSpace(e.BasicProperties.ContentEncoding))
            {
                try
                {
                    enc = Encoding.GetEncoding(e.BasicProperties.ContentEncoding);
                }
                catch (ArgumentException)
                {
                    /* Ignored */
                }
            }

            enc ??= Encoding.UTF8;

            return enc.GetString(e.Body.Span);
        }
    }
}
