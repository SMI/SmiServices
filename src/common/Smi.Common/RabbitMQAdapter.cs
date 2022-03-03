
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.MessagePatterns;
using Smi.Common.Events;
using Smi.Common.Messaging;
using Smi.Common.Options;

namespace Smi.Common
{

// TODO(rkm 2021-12-17) Until we update our RabbitMQ interface
#pragma warning disable CS0618 // Obsolete

    /// <summary>
    /// Adapter for the RabbitMQ API.
    /// </summary>
    public class RabbitMqAdapter : IRabbitMqAdapter
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
                    return _rabbitResources.Any(x => x.Value as ConsumerResources != null);
                }
            }
        }

        public const string RabbitMqRoutingKey_MatchAnything = "#";
        public const string RabbitMqRoutingKey_MatchOneWord = "*";

        public static TimeSpan DefaultOperationTimeout = TimeSpan.FromSeconds(5);

        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        private readonly HostFatalHandler _hostFatalHandler;
        private readonly string _hostId;

        private readonly IConnection _connection;
        private readonly Dictionary<Guid, RabbitResources> _rabbitResources = new Dictionary<Guid, RabbitResources>();
        private readonly object _oResourceLock = new object();

        private const int MinRabbitServerVersionMajor = 3;
        private const int MinRabbitServerVersionMinor = 7;
        private const int MinRabbitServerVersionPatch = 0;

        private const int MaxSubscriptionAttempts = 5;

        private readonly bool _threaded;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionFactory"></param>
        /// <param name="hostId">Identifier for this host instance</param>
        /// <param name="hostFatalHandler"></param>
        /// <param name="threaded"></param>
        public RabbitMqAdapter(IConnectionFactory connectionFactory, string hostId, HostFatalHandler hostFatalHandler = null, bool threaded = false)
        {
            //_threaded = options.ThreadReceivers;
            _threaded = threaded;

            if (_threaded)
            {
                int minWorker, minIOC;
                ThreadPool.GetMinThreads(out minWorker, out minIOC);
                var workers = Math.Max(50, minWorker);
                if (ThreadPool.SetMaxThreads(workers, 50))
                    _logger.Info($"Set Rabbit event concurrency to ({workers:n},50)");
                else
                    _logger.Warn($"Failed to set Rabbit event concurrency to ({workers:n},50)");
            }

            if (string.IsNullOrWhiteSpace(hostId))
                throw new ArgumentException("hostId");
            _hostId = hostId;

            _connection = connectionFactory.CreateConnection(hostId);
            _connection.ConnectionBlocked += (s, a) => _logger.Warn($"ConnectionBlocked (Reason: {a.Reason.ToString()})");
            _connection.ConnectionUnblocked += (s, a) => _logger.Warn("ConnectionUnblocked");

            if (hostFatalHandler == null)
                _logger.Warn("No handler given for fatal events");

            _hostFatalHandler = hostFatalHandler;

            CheckValidServerSettings(_connection);
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

            if (consumerOptions == null)
                throw new ArgumentNullException(nameof(consumerOptions));

            if (!consumerOptions.VerifyPopulated())
                throw new ArgumentException("The given ConsumerOptions has invalid values");

            // Client label is the same for the IConnection and Subscription since we have a separate connection per consumer
            var label = $"{_hostId}::Consumer::{consumerOptions.QueueName}";
            
            var model = _connection.CreateModel();
            model.BasicQos(0, consumerOptions.QoSPrefetchCount, false);

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

            Subscription subscription = null;
            var connected = false;
            var failed = 0;

            while (!connected)
            {
                try
                {
                    subscription = new Subscription(model, consumerOptions.QueueName, consumerOptions.AutoAck, label);
                    connected = true;
                }
                catch (TimeoutException)
                {
                    if (++failed >= MaxSubscriptionAttempts)
                    {
                        _logger.Warn("Retries exceeded, throwing exception");
                        throw;
                    }

                    _logger.Warn($"Timeout when creating Subscription, retrying in 5s...");
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                }
                catch (OperationInterruptedException e)
                {
                    throw new ApplicationException(
                        $"Error when creating subscription on queue \"{consumerOptions.QueueName}\"", e);
                }
                finally
                {
                    if (!connected)
                    {
                        model.Close(200, "StartConsumer - Couldn't create subscription");
                    }
                }
            }

            var taskId = Guid.NewGuid();
            var taskTokenSource = new CancellationTokenSource();

            var consumerTask = new Task(() => Consume(subscription, consumer, taskTokenSource.Token));

            var resources = new ConsumerResources
            {
                Model = model,
                Subscription = subscription,
                ConsumerTask = consumerTask,
                TokenSource = taskTokenSource
            };

            lock (_oResourceLock)
            {
                _rabbitResources.Add(taskId, resources);
            }

            consumer.OnFatal += (s, e) =>
            {
                resources.Shutdown(DefaultOperationTimeout);
                _hostFatalHandler(s, e);
            };

            consumerTask.Start();
            _logger.Debug($"Consumer task started [QueueName={subscription?.QueueName}]");

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
                if (!_rabbitResources.ContainsKey(taskId))
                    throw new ApplicationException("Guid was not found in the task register");

                var res = (ConsumerResources)_rabbitResources[taskId];

                if (!res.Shutdown(timeout))
                    throw new ApplicationException($"Consume task did not exit in time: {res.Subscription.ConsumerTag}");

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

            IProducerModel producerModel;

            try
            {
                producerModel = isBatch ?
                    new BatchProducerModel(producerOptions.ExchangeName, model, props, producerOptions.MaxConfirmAttempts) :
                    new ProducerModel(producerOptions.ExchangeName, model, props, producerOptions.MaxConfirmAttempts);
            }
            catch (Exception)
            {
                model.Close(200, "SetupProducer - Couldn't create ProducerModel");
                throw;
            }

            var resources = new ProducerResources
            {
                Model = model,
                ProducerModel = producerModel,
            };

            lock (_oResourceLock)
            {
                _rabbitResources.Add(Guid.NewGuid(), resources);
            }

            producerModel.OnFatal += (s, ra) =>
            {
                resources.Dispose();
                _hostFatalHandler.Invoke(s, new FatalErrorEventArgs(ra));
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
                _rabbitResources.Add(Guid.NewGuid(), new RabbitResources
                {
                    Model = model
                });
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

            ShutdownCalled = true;

            var exitOk = true;
            var failedToExit = new List<string>();

            lock (_oResourceLock)
            {
                foreach (var res in _rabbitResources.Values)
                {
                    var asConsumerRes = res as ConsumerResources;

                    if (asConsumerRes != null)
                    {
                        exitOk &= asConsumerRes.Shutdown(timeout);
                        failedToExit.Add(asConsumerRes.Subscription.ConsumerTag);
                    }
                    else
                    {
                        res.Dispose();
                    }
                }

                if (!exitOk)
                    throw new ApplicationException($"Some consumer tasks did not exit in time: {string.Join(", ", failedToExit)}");

                _rabbitResources.Clear();
            }
        }

        /// <summary>
        /// Receives any messages sent to the subscription and passes on to the consumer object.
        /// </summary>
        /// <param name="subscription">Subscription to monitor for messages on.</param>
        /// <param name="consumer">Consumer to send messages on to.</param>
        /// <param name="cancellationToken"></param>
        private void Consume(ISubscription subscription, IConsumer consumer, CancellationToken cancellationToken)
        {
            var worklock = new ReaderWriterLockSlim();
            var m = subscription.Model;
            consumer.SetModel(m);

            while (m.IsOpen && !cancellationToken.IsCancellationRequested && !ShutdownCalled)
            {
                BasicDeliverEventArgs e;

                if (subscription.Next(500, out e))
                {
                    if (_threaded)
                    {
                        Task.Run(() =>
                        {
                            worklock.EnterReadLock();
                            try
                            {
                                consumer.ProcessMessage(e);
                            }
                            finally
                            {
                                worklock.ExitReadLock();
                            }
                        }, cancellationToken);
                    }
                    else
                        consumer.ProcessMessage(e);
                }
            }
            if (_threaded)
            {
                // Taking a write lock means waiting for all read locks to
                // release, i.e. all workers have finished
                worklock.EnterWriteLock();

                // Now there are no *new* messages being processed, flush the queue
                consumer.Shutdown();
                worklock.ExitWriteLock();
            }
            worklock.Dispose();

            var reason = "unknown";

            if (cancellationToken.IsCancellationRequested)
                reason = "cancellation was requested";
            else if (ShutdownCalled)
                reason = "shutdown was called";
            else if (!m.IsOpen)
                reason = "channel is closed";

            _logger.Debug("Consumer for {0} exiting ({1})", subscription.QueueName, reason);
        }

        /// <summary>
        /// Checks we can create a basic connection, and also that the minimum RabbitMQ server version is met
        /// </summary>
        private void CheckValidServerSettings(IConnection connection)
        {
            if (!_connection.ServerProperties.ContainsKey("version"))
                throw new ApplicationException("Could not get RabbitMQ server version");

            var version = Encoding.UTF8.GetString((byte[])_connection.ServerProperties["version"]);
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
            public IModel Model { get; set; }

            protected readonly object OResourceLock = new object();

            protected readonly ILogger Logger;

            public RabbitResources()
            {
                Logger = LogManager.GetLogger(GetType().Name);
            }


            public void Dispose()
            {
                lock (OResourceLock)
                {
                    if (Model.IsOpen)
                        Model.Close(200, "Disposed");
                }
            }
        }

        private class ProducerResources : RabbitResources
        {
            public IProducerModel ProducerModel { get; set; }
        }

        private class ConsumerResources : RabbitResources
        {
            public Task ConsumerTask { get; set; }

            public CancellationTokenSource TokenSource { get; set; }

            public ISubscription Subscription { get; set; }


            public bool Shutdown(TimeSpan timeout)
            {
                bool exitOk;
                lock (OResourceLock)
                {
                    TokenSource.Cancel();

                    // Consumer task can't directly shut itself down, as it will block here
                    exitOk = ConsumerTask.Wait(timeout);

                    Subscription.Close();

                    Dispose();
                }

                Logger.Debug($"Consumer task shutdown [QueueName={Subscription.QueueName}]");

                return exitOk;
            }
        }

        #endregion
    }

#pragma warning restore CS0618 // Obsolete

}
