
using DicomTypeTranslation;
using JetBrains.Annotations;
using NLog;
using RabbitMQ.Client;
using Smi.Common.Events;
using Smi.Common.Helpers;
using Smi.Common.Messages;
using Smi.Common.Messaging;
using Smi.Common.Options;
using System;
using System.Diagnostics;

namespace Smi.Common.Execution
{
    public abstract class MicroserviceHost : IMicroserviceHost
    {
        public event HostFatalHandler OnFatal;

        protected readonly string HostProcessName;
        protected readonly int HostProcessID;

        protected readonly GlobalOptions Globals;
        protected readonly ILogger Logger;

        protected readonly IRabbitMqAdapter RabbitMqAdapter;


        private readonly object _oAdapterLock = new object();
        private bool _auxConnectionsCreated;

        private readonly ProducerOptions _fatalLoggingProducerOptions;
        private IProducerModel _fatalLoggingProducer;

        private readonly ControlMessageConsumer _controlMessageConsumer;

        private bool _stopCalled;

        protected readonly MicroserviceObjectFactory ObjectFactory;

        /// <summary>
        /// Loads logging, sets up fatal behaviour, subscribes rabbit etc.
        /// </summary>
        /// <param name="globals">Settings for the microservice (location of rabbit, queue names etc)</param>
        /// <param name="rabbitMqAdapter"></param>
        /// <param name="threaded"></param>
        protected MicroserviceHost(
            [NotNull] GlobalOptions globals,
            IRabbitMqAdapter rabbitMqAdapter = null,
            bool threaded = false)
        {
            if (globals == null || globals.FileSystemOptions == null || globals.RabbitOptions == null || globals.LoggingOptions == null)
                throw new ArgumentException("All or part of the global options are null");

            HostProcessName = SmiCliInit.HostProcessName;

            Logger = LogManager.GetLogger(GetType().Name);
            Logger.Info("Host logger created");

            HostProcessID = Process.GetCurrentProcess().Id;
            Logger.Info($"Starting {HostProcessName} (Host={Environment.MachineName} PID={HostProcessID} User={Environment.UserName})");

            // log centrally
            Globals = globals;
            Logger.Debug("Loaded global options:\n" + globals);

            // should also be centralized for non-host uses
            // Ensure this is false in case the default changes
            DicomTypeTranslater.SerializeBinaryData = false;

            _fatalLoggingProducerOptions = new ProducerOptions
            {
                ExchangeName = Globals.RabbitOptions.FatalLoggingExchange
            };

            //TODO This won't pass for testing with mocked filesystems
            //if(!Directory.Exists(options.FileSystemRoot))
            //    throw new ArgumentException("Could not locate the FileSystemRoot \"" + options.FileSystemRoot + "\"");

            OnFatal += (sender, args) => Fatal(args.Message, args.Exception);

            RabbitMqAdapter = rabbitMqAdapter;
            if (RabbitMqAdapter == null)
            {
                ConnectionFactory connectionFactory = globals.RabbitOptions.CreateConnectionFactory();
                RabbitMqAdapter = new RabbitMqAdapter(connectionFactory, HostProcessName + HostProcessID, OnFatal, threaded);
                _controlMessageConsumer = new ControlMessageConsumer(connectionFactory, HostProcessName, HostProcessID, globals.RabbitOptions.RabbitMqControlExchangeName, this.Stop);
            }

            ObjectFactory = new MicroserviceObjectFactory();
            ObjectFactory.FatalHandler = (s, e) => Fatal(e.Message, e.Exception);
        }

        /// <summary>
        /// Add an event handler to the control message consumer
        /// </summary>
        /// <param name="handler">Method to call when invoked. Parameters are the action to perform, and the message body</param>
        protected void AddControlHandler(IControlMessageHandler handler)
        {
            //(a, m) => action, message content
            _controlMessageConsumer.ControlEvent += handler.ControlMessageHandler;
        }

        /// <summary>
        /// Start this separately so we don't block the thread if the host constructor throws an exception
        /// </summary>
        public void StartAuxConnections()
        {
            lock (_oAdapterLock)
            {
                if (_auxConnectionsCreated)
                    return;

                _auxConnectionsCreated = true;

                // Ensures no consumers have been started until we explicitly call Start()
                if (RabbitMqAdapter.HasConsumers)
                    throw new ApplicationException("Rabbit adapter has consumers before aux. connections created");

                _fatalLoggingProducer = RabbitMqAdapter.SetupProducer(_fatalLoggingProducerOptions, isBatch: false);
                RabbitMqAdapter.StartConsumer(_controlMessageConsumer.ControlConsumerOptions, _controlMessageConsumer, isSolo: false);
            }
        }

        /// <summary>
        /// Per-host implementation. <see cref="IConsumer"/> objects should not be started outside this method
        /// </summary>
        public abstract void Start();

        //TODO Expose timeout here
        public virtual void Stop(string reason)
        {
            Logger.Info("Host Stop called: " + reason);

            if (_stopCalled)
                Logger.Warn("Host stop called twice");

            _stopCalled = true;

            Logger.Debug("Shutting down RabbitMQ connections");

            // Attempt to destroy the control queue

            try
            {
                _controlMessageConsumer.Shutdown();
            }
            catch (Exception e)
            {
                Logger.Warn("Could not clean up control queues: " + e.Message);
            }

            lock (_oAdapterLock)
            {
                RabbitMqAdapter.Shutdown(Common.RabbitMqAdapter.DefaultOperationTimeout);
            }

            Logger.Info("Host stop completed");

            // Always remember to flush!
            LogManager.Shutdown();
        }

        /// <summary>
        /// Fatal essentially just calls <see cref="Stop"/>, but attempts to send a FatalErrorMessage to RabbitMQ first
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="exception"></param>
        public void Fatal(string msg, Exception exception)
        {
            if (_stopCalled)
                return;

            Logger.Fatal(exception, msg);

            if (_fatalLoggingProducer != null)
                try
                {
                    _fatalLoggingProducer.SendMessage(new FatalErrorMessage(msg, exception), null);
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Failed to log fatal error");
                }

            Stop("Fatal error in MicroserviceHost (" + msg + ")");
        }
    }
}
