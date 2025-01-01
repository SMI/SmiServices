using DicomTypeTranslation;
using FellowOakDicom;
using NLog;
using SmiServices.Common.Events;
using SmiServices.Common.Helpers;
using SmiServices.Common.Messages;
using SmiServices.Common.Messaging;
using SmiServices.Common.Options;
using System;

namespace SmiServices.Common.Execution
{
    public abstract class MicroserviceHost : IMicroserviceHost
    {
        public event HostFatalHandler OnFatal;

        protected readonly string HostProcessName;
        protected readonly int HostProcessID;

        protected readonly GlobalOptions Globals;
        protected readonly ILogger Logger;

        protected readonly IMessageBroker MessageBroker;


        private readonly object _oAdapterLock = new();
        private bool _auxConnectionsCreated;

        private readonly ProducerOptions _fatalLoggingProducerOptions;
        private IProducerModel? _fatalLoggingProducer;

        private readonly ControlMessageConsumer _controlMessageConsumer = null!;

        private bool _stopCalled;

        protected readonly MicroserviceObjectFactory ObjectFactory;

        /// <summary>
        /// Loads logging, sets up fatal behaviour, subscribes rabbit etc.
        /// </summary>
        /// <param name="globals">Settings for the microservice (location of rabbit, queue names etc)</param>
        /// <param name="messageBroker"></param>
        protected MicroserviceHost(
            GlobalOptions globals,
            IMessageBroker? messageBroker = null)
        {
            if (globals == null || globals.FileSystemOptions == null || globals.RabbitOptions == null || globals.LoggingOptions == null)
                throw new ArgumentException("All or part of the global options are null");

            // Disable fo-dicom's DICOM validation globally from here
            new DicomSetupBuilder().SkipValidation();

            HostProcessName = globals.HostProcessName;

            Logger = LogManager.GetLogger(GetType().Name);
            Logger.Info("Host logger created");

            HostProcessID = Environment.ProcessId;
            Logger.Info($"Starting {HostProcessName} (Host={Environment.MachineName} PID={HostProcessID} User={Environment.UserName})");

            // log centrally
            Globals = globals;
            Logger.Debug($"Loaded global options:\n{globals}");

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

            if (messageBroker == null)
            {
                messageBroker = new RabbitMQBroker(globals.RabbitOptions, HostProcessName + HostProcessID, OnFatal);
                var controlExchangeName = globals.RabbitOptions.RabbitMqControlExchangeName ?? throw new ArgumentNullException(nameof(globals));
                _controlMessageConsumer = new ControlMessageConsumer(globals.RabbitOptions, HostProcessName, HostProcessID, controlExchangeName, Stop);
            }
            MessageBroker = messageBroker;

            ObjectFactory = new MicroserviceObjectFactory
            {
                FatalHandler = (s, e) => Fatal(e.Message, e.Exception)
            };
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
                if (MessageBroker.HasConsumers)
                    throw new ApplicationException("Rabbit adapter has consumers before aux. connections created");

                _fatalLoggingProducer = MessageBroker.SetupProducer(_fatalLoggingProducerOptions, isBatch: false);
                MessageBroker.StartControlConsumer(_controlMessageConsumer);
            }
        }

        /// <summary>
        /// Per-host implementation. <see cref="IConsumer{T}"/> objects should not be started outside this method
        /// </summary>
        public abstract void Start();

        //TODO Expose timeout here
        public virtual void Stop(string reason)
        {
            Logger.Info($"Host Stop called: {reason}");

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
                Logger.Warn($"Could not clean up control queues: {e.Message}");
            }

            lock (_oAdapterLock)
            {
                MessageBroker.Shutdown(RabbitMQBroker.DefaultOperationTimeout);
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
        public void Fatal(string msg, Exception? exception)
        {
            Logger.Fatal(exception, msg);
            if (_stopCalled)
                return;

            try
            {
                _fatalLoggingProducer?.SendMessage(new FatalErrorMessage(msg, exception), null, null);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to log fatal error");
            }

            Stop($"Fatal error in MicroserviceHost ({msg})");
        }

        public void Wait()
        {
            MessageBroker.Wait();
        }
    }
}
