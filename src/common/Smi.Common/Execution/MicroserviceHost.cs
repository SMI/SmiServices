
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
using System.IO;
using System.Reflection;

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
        /// <param name="loadSmiLogConfig">True to replace any existing <see cref="LogManager.Configuration"/> with the SMI logging configuration (which must exist in the file "Smi.NLog.config" of the current directory)</param>
        /// <param name="threaded"></param>
        protected MicroserviceHost(
            [NotNull] GlobalOptions globals,
            IRabbitMqAdapter rabbitMqAdapter = null,
            bool loadSmiLogConfig = true,
            bool threaded = false)
        {
            if (globals == null || globals.FileSystemOptions == null || globals.RabbitOptions == null || globals.MicroserviceOptions == null)
                throw new ArgumentException("All or part of the global options are null");

            HostProcessName = Assembly.GetEntryAssembly()?.GetName().Name ?? throw new ApplicationException("Couldn't get the Assembly name!");

            string logConfigPath = null;

            // We may not want to do this during tests, however this should always be true otherwise
            if (loadSmiLogConfig)
            {
                logConfigPath = !string.IsNullOrWhiteSpace(globals.FileSystemOptions.LogConfigFile)
                    ? globals.FileSystemOptions.LogConfigFile
                    : Path.Combine(globals.CurrentDirectory, "Smi.NLog.config");

                if (!File.Exists(logConfigPath))
                    throw new FileNotFoundException("Could not find the logging configuration in the current directory (Smi.NLog.config), or at the path specified by FileSystemOptions.LogConfigFile");

                LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration(logConfigPath, false);

                if (globals.FileSystemOptions.ForceSmiLogsRoot)
                {
                    string smiLogsRoot = globals.LogsRoot;

                    if (string.IsNullOrWhiteSpace(smiLogsRoot) || !Directory.Exists(smiLogsRoot))
                        throw new ApplicationException($"Invalid logs root: {smiLogsRoot}");

                    LogManager.Configuration.Variables["baseFileName"] =
                        $"{smiLogsRoot}/{HostProcessName}/${{cached:cached=true:clearCache=None:inner=${{date:format=yyyy-MM-dd-HH-mm-ss}}}}-${{processid}}";
                }
            }

            Logger = LogManager.GetLogger(GetType().Name);
            Logger.Info("Host logger created with " + (loadSmiLogConfig ? "SMI" : "existing") + " logging config");

            if (!string.IsNullOrWhiteSpace(logConfigPath))
                Logger.Debug($"Logging config loaded from {logConfigPath}");

            if (!globals.MicroserviceOptions.TraceLogging)
                LogManager.GlobalThreshold = LogLevel.Debug;

            Logger.Trace("Trace logging enabled!");

            HostProcessID = Process.GetCurrentProcess().Id;
            Logger.Info($"Starting {HostProcessName} (Host={Environment.MachineName} PID={HostProcessID} User={Environment.UserName})");

            Globals = globals;
            Logger.Debug("Loaded global options:\n" + globals);

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
