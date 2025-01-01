using NLog;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using SmiServices.Common.Events;
using SmiServices.Common.Options;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SmiServices.Common.Messaging
{
    public class ControlMessageConsumer : IControlMessageConsumer
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public ConsumerOptions ControlConsumerOptions { get => _controlConsumerOptions; }

        private readonly ConsumerOptions _controlConsumerOptions = new()
        {
            QoSPrefetchCount = 1,
            AutoAck = true
        };

        public event StopEventHandler StopHost;
        public event ControlEventHandler? ControlEvent;


        private readonly string _processName;
        private readonly string _processId;
        private readonly IConnection _connection;

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

            _connection = rabbitOptions.Connection;
            _processName = processName.ToLower();
            _processId = processId.ToString();

            _controlConsumerOptions.QueueName = $"Control.{_processName}{_processId}";

            SetupControlQueueForHost(controlExchangeName);

            StopHost += () => stopEvent("Control message stop");
        }

        /// <summary>
        /// Recreate ProcessMessage to specifically handle control messages which won't have headers,
        /// and shouldn't be included in any Ack/Nack counts
        /// </summary>
        public void ProcessMessage(string body, string routingKey)
        {
            try
            {
                // For now we only deal with the simple case of "smi.control.<who>.<what>". Can expand later on depending on our needs
                // Queues will be deleted when the connection is closed so don't need to worry about messages being leftover

                _logger.Info("Control message received with routing key: " + routingKey);

                string[] split = routingKey.ToLower().Split('.');

                if (split.Length < 4)
                {
                    _logger.Debug("Control command shorter than the minimum format");
                    return;
                }

                // Who, what
                string actor = string.Join(".", split.Skip(2).Take(split.Length - 3));
                string action = split[^1];

                // If action contains a numeric and it's not our PID, then ignore
                if (action.Any(char.IsDigit) && !action.EndsWith(_processId))
                    return;

                // Ignore any messages not meant for us
                if (!actor.Equals("all") && !actor.Equals(_processName))
                {
                    _logger.Debug("Control command did not match this service");
                    return;
                }

                // Handle any general actions - just stop and ping for now

                if (action.StartsWith("stop"))
                {
                    if (StopHost == null)
                    {
                        // This should never really happen
                        _logger.Info("Received stop command but no stop event registered");
                        return;
                    }

                    _logger.Info("Stop request received, raising StopHost event");
                    Task.Run(() => StopHost.Invoke());

                    return;
                }

                if (action.StartsWith("ping"))
                {
                    _logger.Info("Pong!");
                    return;
                }

                // Don't pass any unhandled broadcast (to "all") messages down to the hosts
                if (actor.Equals("all"))
                    return;

                // Else raise the event if any hosts have specific control needs
                if (ControlEvent != null)
                {
                    _logger.Debug("Control message not handled, raising registered ControlEvent(s)");
                    ControlEvent(Regex.Replace(action, @"[\d]", ""), body);

                    return;
                }

                // Else we should ignore it?
                _logger.Warn("Unhandled control message with routing key: " + routingKey);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "ProcessMessageImpl threw unhandled exception");
            }
        }

        /// <summary>
        /// Ensures the control queue is cleaned up on exit. Should have been deleted already, but this ensures it
        /// </summary>
        public void Shutdown()
        {
            using var model = _connection.CreateModel();
            _logger.Debug($"Deleting control queue: {_controlConsumerOptions.QueueName}");
            model.QueueDelete(_controlConsumerOptions.QueueName);
        }

        /// <summary>
        /// Creates a one-time connection to set up the required control queue and bindings on the RabbitMQ server.
        /// The connection is disposed and StartConsumer(...) can then be called on the parent MessageBroker with ControlConsumerOptions
        /// </summary>
        /// <param name="controlExchangeName"></param>
        private void SetupControlQueueForHost(string controlExchangeName)
        {
            using var model = _connection.CreateModel();
            try
            {
                model.ExchangeDeclarePassive(controlExchangeName);
            }
            catch (OperationInterruptedException e)
            {
                throw new ApplicationException($"The given control exchange was not found on the server: \"{controlExchangeName}\"", e);
            }

            _logger.Debug($"Creating control queue {_controlConsumerOptions.QueueName}");

            // Declare our queue with:
            // durable = false (queue will not persist over restarts of the RabbitMq server)
            // exclusive = false (queue won't be deleted when THIS connection closes)
            // autoDelete = true (queue will be deleted after a consumer connects and then disconnects)
            model.QueueDeclare(_controlConsumerOptions.QueueName, durable: false, exclusive: false, autoDelete: true);

            // Binding for any control requests, i.e. "stop"
            _logger.Debug($"Creating binding {controlExchangeName}->{_controlConsumerOptions.QueueName} with key {ControlQueueBindingKey}");
            model.QueueBind(_controlConsumerOptions.QueueName, controlExchangeName, ControlQueueBindingKey);

            // Specific microservice binding key, ignoring the id at the end of the process name
            string bindingKey = $"smi.control.{_processName}.*";

            _logger.Debug($"Creating binding {controlExchangeName}->{_controlConsumerOptions.QueueName} with key {bindingKey}");
            model.QueueBind(_controlConsumerOptions.QueueName, controlExchangeName, bindingKey);
        }
    }
}
