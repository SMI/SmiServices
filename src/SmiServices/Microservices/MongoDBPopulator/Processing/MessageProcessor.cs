using MongoDB.Bson;
using NLog;
using RabbitMQ.Client;
using Smi.Common.Messages;
using Smi.Common.Options;
using System;
using System.Collections.Generic;
using SysTimers = System.Timers;

namespace SmiServices.Microservices.MongoDBPopulator.Processing
{
    /// <inheritdoc />
    /// <summary>
    /// Abstract class containing the common functionality of both the processor classes
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class MessageProcessor<T> : IMessageProcessor<T> where T : IMessage
    {
        #region Abstract Fields/Properties

        /// <inheritdoc />
        public abstract void AddToWriteQueue(T message, IMessageHeader header, ulong deliveryTag);

        /// <inheritdoc />
        /// <summary>
        /// Stop the processing of further messages
        /// </summary>
        /// <param name="reason"></param>
        public abstract void StopProcessing(string reason);

        protected abstract void ProcessQueue();

        #endregion

        #region Concrete Fields/Properties

        /// <inheritdoc />
        /// <summary>
        /// Model to use when sending ACK for messages
        /// </summary>
        public IModel? Model { get; set; }

        /// <inheritdoc />
        /// <summary>
        /// Indicates if the object is actively processing messages
        /// </summary>
        public bool IsStopping { get; private set; }

        /// <inheritdoc />
        /// <summary>
        /// Total number of messages acknowledged during the processor's lifetime
        /// </summary>
        public int AckCount { get; protected set; }

        //TODO Check this is named properly in subclasses
        protected readonly ILogger Logger;

        protected readonly IMongoDbAdapter MongoDbAdapter;

        // Keeps track of the number of consecutive times we have failed a write attempt
        protected int FailedWriteAttempts;
        protected readonly int FailedWriteLimit;

        protected readonly Queue<Tuple<BsonDocument, ulong>> ToProcess = new();
        protected readonly int MaxQueueSize;
        protected readonly object LockObj = new();
        private readonly SysTimers.Timer _processTimer;

        private readonly Action<Exception> _exceptionCallback;

        // Set the max size to 16MB minus some overhead
        protected const int MaxDocumentSize = 16 * 1024 * 1024 - 512;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="options"></param>
        /// <param name="mongoDbAdapter"></param>
        /// <param name="maxQueueSize"></param>
        /// <param name="exceptionCallback"></param>
        protected MessageProcessor(MongoDbPopulatorOptions options, IMongoDbAdapter mongoDbAdapter, int maxQueueSize, Action<Exception> exceptionCallback)
        {
            Logger = LogManager.GetLogger(GetType().Name);

            _exceptionCallback = exceptionCallback;

            MongoDbAdapter = mongoDbAdapter;
            FailedWriteLimit = options.FailedWriteLimit;

            MaxQueueSize = maxQueueSize;

            _processTimer = new SysTimers.Timer(Math.Min(int.MaxValue, (double)options.MongoDbFlushTime * 1000));
            _processTimer.Elapsed += TimerElapsedEvent;
            _processTimer.Start();

            IsStopping = false;
        }

        private void TimerElapsedEvent(object? source, SysTimers.ElapsedEventArgs e)
        {
            try
            {
                ProcessQueue();
            }
            catch (Exception ex)
            {
                StopProcessing("Timed ProcessQueue threw an exception");
                _exceptionCallback(ex);
            }
        }

        protected void StopProcessing()
        {
            // Ensures no more messages are added to the queue
            _processTimer.Stop();
            IsStopping = true;

            // Forces process to wait until any current processing is finished
            lock (LockObj)
                Logger.Debug("Lock released, no more messages will be processed");
        }

        #endregion
    }
}
