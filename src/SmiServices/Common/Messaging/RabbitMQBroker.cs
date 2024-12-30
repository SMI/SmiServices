using NLog;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using SmiServices.Common.Events;
using SmiServices.Common.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace SmiServices.Common.Messaging
{
    /// <summary>
    /// Adapter for the RabbitMQ API.
    /// </summary>
    public class RabbitMQBroker : IMessageBroker
    {
        /// <summary>
        /// Used to ensure we can't create any new connections after we have called Shutdown()
        /// </summary>
        public bool ShutdownCalled { get; private set; }

        public bool HasConsumers
        {
            get
            {
                lock (_oResourceLock)
                {
                    return _rabbitResources.Any(x => x.Value is ConsumerResources);
                }
            }
        }

        public const string RabbitMqRoutingKey_MatchAnything = "#";
        public const string RabbitMqRoutingKey_MatchOneWord = "*";

        public static readonly TimeSpan DefaultOperationTimeout = TimeSpan.FromSeconds(5);

        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        private readonly HostFatalHandler? _hostFatalHandler;

        private readonly IConnection _connection;
        private readonly Dictionary<Guid, RabbitResources> _rabbitResources = [];
        private readonly object _oResourceLock = new();
        private readonly object _exitLock = new();

        private const int MinRabbitServerVersionMajor = 3;
        private const int MinRabbitServerVersionMinor = 7;
        private const int MinRabbitServerVersionPatch = 0;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rabbitOptions"></param>
        /// <param name="hostId">Identifier for this host instance</param>
        /// <param name="hostFatalHandler"></param>
        /// <param name="threaded"></param>
        public RabbitMQBroker(RabbitOptions rabbitOptions, string hostId, HostFatalHandler? hostFatalHandler = null, bool threaded = false)
        {
            if (threaded)
            {
                ThreadPool.GetMinThreads(out var minWorker, out var minIOC);
                var workers = Math.Max(50, minWorker);
                if (ThreadPool.SetMaxThreads(workers, 50))
                    _logger.Info($"Set Rabbit event concurrency to ({workers:n},50)");
                else
                    _logger.Warn($"Failed to set Rabbit event concurrency to ({workers:n},50)");
            }

            if (string.IsNullOrWhiteSpace(hostId))
                throw new ArgumentException("RabbitMQ host ID required", nameof(hostId));

            _connection = rabbitOptions.Connection;
            _connection.ConnectionBlocked += (s, a) => _logger.Warn($"ConnectionBlocked (Reason: {a.Reason})");
            _connection.ConnectionUnblocked += (s, a) => _logger.Warn("ConnectionUnblocked");

            if (hostFatalHandler == null)
                _logger.Warn("No handler given for fatal events");

            _hostFatalHandler = hostFatalHandler;

            CheckValidServerSettings();
        }


        /// <summary>
        /// Setup a subscription to a queue which sends messages to the <see cref="IConsumer"/>.
        /// </summary>
        /// <param name="consumerOptions">The connection options.</param>
        /// <param name="consumer">Consumer that will be sent any received messages.</param>
        /// <param name="isSolo">If specified, will ensure that it is the only consumer on the provided queue</param>
        /// <returns>Identifier for the consumer task, can be used to stop the consumer without shutting down the whole adapter</returns>
        public Guid StartConsumer(ConsumerOptions consumerOptions, IConsumer consumer, bool isSolo = false)
        {
            if (ShutdownCalled)
                throw new ApplicationException("Adapter has been shut down");

            ArgumentNullException.ThrowIfNull(consumerOptions);

            if (!consumerOptions.VerifyPopulated())
                throw new ArgumentException("The given ConsumerOptions has invalid values");

            var model = _connection.CreateModel();
            model.BasicQos(0, consumerOptions.QoSPrefetchCount, false);
            consumer.QoSPrefetchCount = consumerOptions.QoSPrefetchCount;

            // Check queue exists
            try
            {
                // Passively declare the queue (equivalent to checking the queue exists)
                model.QueueDeclarePassive(consumerOptions.QueueName);
            }
            catch (OperationInterruptedException e)
            {
                model.Close(200, "StartConsumer - Queue missing");
                throw new ApplicationException($"Expected queue \"{consumerOptions.QueueName}\" to exist", e);
            }

            if (isSolo && model.ConsumerCount(consumerOptions.QueueName) > 0)
            {
                model.Close(200, "StartConsumer - Already a consumer on the queue");
                throw new ApplicationException($"Already a consumer on queue {consumerOptions.QueueName} and solo consumer was specified");
            }

            consumer.SetModel(model);
            EventingBasicConsumer ebc = new(model);
            ebc.Received += (o, a) =>
            {
                consumer.ProcessMessage(a);
            };
            void shutdown(object? o, ShutdownEventArgs a)
            {
                var reason = "cancellation was requested";
                if (ebc.Model.IsClosed)
                    reason = "channel is closed";
                if (ShutdownCalled)
                    reason = "shutdown was called";
                _logger.Debug($"Consumer for {consumerOptions.QueueName} exiting ({reason})");
            }
            model.ModelShutdown += shutdown;
            ebc.Shutdown += shutdown;

            var resources = new ConsumerResources(ebc, consumerOptions.QueueName!, model);
            Guid taskId = Guid.NewGuid();

            lock (_oResourceLock)
            {
                _rabbitResources.Add(taskId, resources);
            }

            consumer.OnFatal += (s, e) =>
            {
                resources.Dispose();
                _hostFatalHandler?.Invoke(s, e);
            };

            if (consumerOptions.HoldUnprocessableMessages && !consumerOptions.AutoAck)
                consumer.HoldUnprocessableMessages = true;

            consumer.OnAck += (_, a) => { ebc.Model.BasicAck(a.DeliveryTag, a.Multiple); };
            consumer.OnNack += (_, a) => { ebc.Model.BasicNack(a.DeliveryTag, a.Multiple, a.Requeue); };

            model.BasicConsume(ebc, consumerOptions.QueueName, consumerOptions.AutoAck);
            _logger.Debug($"Consumer task started [QueueName={consumerOptions?.QueueName}]");
            return taskId;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="taskId"></param>
        /// <param name="timeout"></param>
        public void StopConsumer(Guid taskId, TimeSpan timeout)
        {
            if (ShutdownCalled)
                return;

            lock (_oResourceLock)
            {
                if (!_rabbitResources.TryGetValue(taskId, out RabbitResources? value))
                    throw new ApplicationException("Guid was not found in the task register");
                value.Dispose();
                _rabbitResources.Remove(taskId);
            }
        }

        /// <summary>
        /// Setup a <see cref="IProducerModel"/> to send messages with.
        /// </summary>
        /// <param name="producerOptions">The producer options class to setup which must include the exchange name.</param>
        /// <param name="isBatch"></param>
        /// <returns>Object which can send messages to a RabbitMQ exchange.</returns>
        public IProducerModel SetupProducer(ProducerOptions producerOptions, bool isBatch = false)
        {
            if (ShutdownCalled)
                throw new ApplicationException("Adapter has been shut down");

            if (!producerOptions.VerifyPopulated())
                throw new ArgumentException("The given producer options have invalid values");

            //NOTE: IModel objects are /not/ thread safe
            var model = _connection.CreateModel();
            model.ConfirmSelect();

            try
            {
                // Passively declare the exchange (equivalent to checking the exchange exists)
                model.ExchangeDeclarePassive(producerOptions.ExchangeName);
            }
            catch (OperationInterruptedException e)
            {
                model.Close(200, "SetupProducer - Exchange missing");
                throw new ApplicationException($"Expected exchange \"{producerOptions.ExchangeName}\" to exist", e);
            }

            var props = model.CreateBasicProperties();
            props.ContentEncoding = "UTF-8";
            props.ContentType = "application/json";
            props.Persistent = true;

            IBackoffProvider? backoffProvider = null;
            if (producerOptions.BackoffProviderType != null)
            {
                try
                {
                    backoffProvider = BackoffProviderFactory.Create(producerOptions.BackoffProviderType);
                }
                catch (Exception)
                {
                    model.Close(200, "SetupProducer - Couldn't create BackoffProvider");
                    throw;
                }
            }

            IProducerModel producerModel;
            try
            {
                producerModel = isBatch ?
                    new BatchProducerModel(producerOptions.ExchangeName!, model, props, producerOptions.MaxConfirmAttempts, backoffProvider, producerOptions.ProbeQueueName, producerOptions.ProbeQueueLimit, producerOptions.ProbeTimeout) :
                    new ProducerModel(producerOptions.ExchangeName!, model, props, producerOptions.MaxConfirmAttempts, backoffProvider, producerOptions.ProbeQueueName, producerOptions.ProbeQueueLimit, producerOptions.ProbeTimeout);
            }
            catch (Exception)
            {
                model.Close(200, "SetupProducer - Couldn't create ProducerModel");
                throw;
            }

            var resources = new ProducerResources(model, producerModel);
            lock (_oResourceLock)
            {
                _rabbitResources.Add(Guid.NewGuid(), resources);
            }

            producerModel.OnFatal += (s, ra) =>
            {
                resources.Dispose();
                _hostFatalHandler?.Invoke(s, new FatalErrorEventArgs(ra));
            };

            return producerModel;
        }

        /// <summary>
        /// Get a blank model with no options set
        /// </summary>
        /// <param name="connectionName"></param>
        /// <returns></returns>
        public IModel GetModel(string connectionName)
        {
            //TODO This method has no callback available for fatal errors

            if (ShutdownCalled)
                throw new ApplicationException("Adapter has been shut down");

            var model = _connection.CreateModel();

            lock (_oResourceLock)
            {
                _rabbitResources.Add(Guid.NewGuid(), new RabbitResources(model));
            }

            return model;
        }

        /// <summary>
        /// Close all open connections and stop any consumers
        /// </summary>
        /// <param name="timeout">Max time given for each consumer to exit</param>
        public void Shutdown(TimeSpan timeout)
        {
            if (ShutdownCalled)
                return;
            if (timeout.Equals(TimeSpan.Zero))
                throw new ApplicationException($"Invalid {nameof(timeout)} value");

            ShutdownCalled = true;

            lock (_oResourceLock)
            {
                foreach (var res in _rabbitResources.Values)
                {
                    res.Dispose();
                }
                _rabbitResources.Clear();
            }
            lock (_exitLock)
                Monitor.PulseAll(_exitLock);
        }

        /// <summary>
        /// Checks that the minimum RabbitMQ server version is met
        /// </summary>
        private void CheckValidServerSettings()
        {
            if (!_connection.ServerProperties.TryGetValue("version", out object? value))
                throw new ApplicationException("Could not get RabbitMQ server version");

            var version = Encoding.UTF8.GetString((byte[])value);
            var split = version.Split('.');

            if (int.Parse(split[0]) < MinRabbitServerVersionMajor ||
                (int.Parse(split[0]) == MinRabbitServerVersionMajor &&
                 int.Parse(split[1]) < MinRabbitServerVersionMinor) ||
                (int.Parse(split[0]) == MinRabbitServerVersionMajor &&
                 int.Parse(split[1]) == MinRabbitServerVersionMinor &&
                 int.Parse(split[2]) < MinRabbitServerVersionPatch))
            {
                throw new ApplicationException(
                    $"Connected to RabbitMQ server version {version}, but minimum required is {MinRabbitServerVersionMajor}.{MinRabbitServerVersionMinor}.{MinRabbitServerVersionPatch}");
            }

            _logger.Debug($"Connected to RabbitMQ server version {version}");
        }

        #region Resource Classes

        private class RabbitResources : IDisposable
        {
            public IModel Model { get; }

            protected readonly object OResourceLock = new();

            protected readonly ILogger Logger;

            public RabbitResources(IModel model)
            {
                Logger = LogManager.GetLogger(GetType().Name);
                Model = model;
            }

            public virtual void Dispose()
            {
                if (Model.IsOpen)
                    Model.Close();
            }
        }

        private class ProducerResources : RabbitResources
        {
            public IProducerModel? ProducerModel { get; set; }

            public ProducerResources(IModel model, IProducerModel ipm) : base(model)
            {
                ProducerModel = ipm;
            }
        }

        private class ConsumerResources : RabbitResources
        {
            internal readonly EventingBasicConsumer ebc;
            internal readonly string QueueName;

            public override void Dispose()
            {
                foreach (var tag in ebc.ConsumerTags)
                {
                    Model.BasicCancel(tag);
                }
                if (!Model.IsOpen)
                    return;
                Model.Close();
                Model.Dispose();
            }

            internal ConsumerResources(EventingBasicConsumer ebc, string q, IModel model) : base(model)
            {
                this.ebc = ebc;
                this.QueueName = q;
            }
        }

        #endregion

        public void Wait()
        {
            lock (_exitLock)
            {
                while (!ShutdownCalled)
                {
                    Monitor.Wait(_exitLock);
                }
            }
        }
    }

}
