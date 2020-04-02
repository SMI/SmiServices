﻿using Dicom;
using DicomTypeTranslation;
using Smi.Common.Messages;
using Smi.Common.Messaging;
using Smi.Common.Options;
using Microservices.DicomRelationalMapper.Execution;
using Microservices.DicomRelationalMapper.Execution.Namers;
using NLog;
using RabbitMQ.Client.Events;
using Rdmp.Core.Curation.Data.DataLoad;
using Rdmp.Core.Curation.Data.EntityNaming;
using Rdmp.Core.DataLoad;
using Rdmp.Core.DataLoad.Engine.Checks.Checkers;
using Rdmp.Core.DataLoad.Engine.DatabaseManagement.EntityNaming;
using Rdmp.Core.Repositories;
using ReusableLibraryCode.Checks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microservices.DicomRelationalMapper.Messaging
{
    public class DicomRelationalMapperQueueConsumer : Consumer, IDisposable
    {
        //TODO This is literally only public for testing purposes
        public INameDatabasesAndTablesDuringLoads DatabaseNamer { get; private set; }

        public int MessagesProcessed { get { return NackCount + AckCount; } }
        
        /// <summary>
        /// Collection of all DLE crash messages (including those where successful restart runs were performed).
        /// </summary>
        public IReadOnlyCollection<Exception> DleErrors => new ReadOnlyCollection<Exception>(_dleExceptions);
        
        private List<Exception> _dleExceptions = new List<Exception>();

        private readonly LoadMetadata _lmd;
        private readonly IRDMPPlatformRepositoryServiceLocator _repositoryLocator;

        private DateTime _lastRanDle = DateTime.Now;

        // Unprocessed messages awaiting an opportunity to be run
        private readonly Queue<QueuedImage> _imageQueue = new Queue<QueuedImage>();
        private readonly object _oQueueLock = new object();

        private bool _stopCalled;

        private readonly int _minimumBatchSize;
        private readonly bool _useInsertIntoForRawMigration;
        private readonly int _retryOnFailureCount;
        private readonly int _retryDelayInSeconds;

        /// <summary>
        /// The maximum number of seconds to wait for MinimumBatchSize to be reached before emptying the Queue anyway
        /// </summary>
        private readonly TimeSpan _maximumRunDelayInSeconds;

        private Task _dleTask;
        private readonly CancellationTokenSource _stopTokenSource = new CancellationTokenSource();


        /// <summary>
        /// True to run <see cref="PreExecutionChecker"/> before the data load accepting all proposed fixes (e.g. dropping RAW)
        /// <para>Default is false</para>
        /// </summary>
        public bool RunChecks { get; set; }


        public DicomRelationalMapperQueueConsumer(IRDMPPlatformRepositoryServiceLocator repositoryLocator, LoadMetadata lmd, INameDatabasesAndTablesDuringLoads namer, DicomRelationalMapperOptions options)
        {
            _lmd = lmd;
            _repositoryLocator = repositoryLocator;
            DatabaseNamer = namer;

            _minimumBatchSize = options.MinimumBatchSize;
            _useInsertIntoForRawMigration = options.UseInsertIntoForRAWMigration;
            _retryOnFailureCount = options.RetryOnFailureCount;
            _retryDelayInSeconds = Math.Max(10,options.RetryDelayInSeconds);
            _maximumRunDelayInSeconds = new TimeSpan(0, 0, 0, options.MaximumRunDelayInSeconds <= 0 ? 15 : 0);

            StartDleRunnerTask();
        }


        protected override void ProcessMessageImpl(IMessageHeader header, BasicDeliverEventArgs deliverEventArgs)
        {
            DicomFileMessage message;
            if (!SafeDeserializeToMessage(header, deliverEventArgs, out message))
                return;

            DicomDataset dataset;

            try
            {
                dataset = DicomTypeTranslater.DeserializeJsonToDataset(message.DicomDataset);
            }
            catch (Exception e)
            {
                ErrorAndNack(header, deliverEventArgs, "Could not rebuild DicomDataset from message", e);
                return;
            }

            var toQueue = new QueuedImage(header, deliverEventArgs, message, dataset);

            lock (_oQueueLock)
                _imageQueue.Enqueue(toQueue);
        }

        public void Stop(string reason)
        {
            Logger.Debug("Stop called: {0}", reason);

            if (_stopCalled)
            {
                Logger.Warn("Stop called twice");
                return;
            }

            _stopCalled = true;

            // Cancel the DLE runner task and wait for it to exit. This will deadlock if the DLE task ever calls Stop directly
            _stopTokenSource.Cancel();
            _dleTask.Wait();

            var createAndDestroyStaging = DatabaseNamer as ICreateAndDestroyStagingDuringLoads;

            if (createAndDestroyStaging != null)
                createAndDestroyStaging.DestroyStagingIfExists();
        }

        private void StartDleRunnerTask()
        {
            _dleTask = Task.Factory.StartNew(() =>
            {
                Exception faultCause = null;

                while (!_stopTokenSource.IsCancellationRequested)
                {
                    try
                    {
                        RunDleIfRequired();
                    }
                    catch (Exception e)
                    {
                        // Handles any exceptions not caused by the DLE returning an error code
                        _stopTokenSource.Cancel();
                        faultCause = e;
                        _dleExceptions.Add(e);
                        Logger.Log(LogLevel.Error,e,"DLE crashed during RunDleIfRequired");
                    }
                }

                if (faultCause != null)
                    Fatal("Unhandled exception in DLE runner task", faultCause);

                Logger.Debug("DLE runner task exiting");
            });

            Logger.Debug("DLE task started");
        }

        private void RunDleIfRequired()
        {
            //if there are a decent number ready to go or we haven't run in a while (and there is at least 1)
            if (GetQueueCount() < (DateTime.Now.Subtract(_lastRanDle) > _maximumRunDelayInSeconds ? 1 : _minimumBatchSize))
                return;

            var toProcess = new List<QueuedImage>();
            var duplicates = new List<QueuedImage>();
            var seenSoFar = new HashSet<string>();

            // Get the messages we will start this DLE with, accounting for duplicates
            lock (_oQueueLock)
            {
                while (_imageQueue.Count > 0)
                {
                    QueuedImage queuedImage = _imageQueue.Dequeue();

                    if (seenSoFar.Contains(queuedImage.DicomFileMessage.DicomDataset))
                    {
                        duplicates.Add(queuedImage);
                    }
                    else
                    {
                        toProcess.Add(queuedImage);
                        seenSoFar.Add(queuedImage.DicomFileMessage.DicomDataset);
                    }
                }
            }

            //All messages were rejected
            if (!toProcess.Any())
                return;

            if (duplicates.Any())
            {
                Logger.Log(LogLevel.Warn, "Acking " + duplicates.Count + " duplicate Datasets");
                duplicates.ForEach(x => Ack(x.Header, x.BasicDeliverEventArgs));
            }

            var parallelDleHost = new ParallelDLEHost(_repositoryLocator, DatabaseNamer, _useInsertIntoForRawMigration);
            Logger.Info("Starting DLE with " + toProcess.Count + " messages");

            if (RunChecks)
                RunDleChecks();

            int remainingRetries = _retryOnFailureCount;
            Exception firstException = null;

            ExitCodeType exitCode;

            var datasetProvider = new DicomFileMessageToDatasetListWorklist(toProcess);

            do
            {
                Logger.Debug("Starting a ParallelDLEHost");

                // We last ran now!
                _lastRanDle = DateTime.Now;
                
                //reset the progress e.g. if we crashed later on in the load
                datasetProvider.ResetProgress();

                try
                {
                    exitCode = parallelDleHost.RunDLE(_lmd, datasetProvider);
                }
                catch (Exception e)
                {
                    Logger.Debug(e,"ParallelDLEHost threw exception of type " + e.GetType());
                    _dleExceptions.Add(e);
                    exitCode = ExitCodeType.Error;

                    if (remainingRetries > 0)
                    {
                        //wait a random length of time averaging the _retryDelayInSeconds to avoid retrying at the same time as other processes
                        //where there is resource contention that results in simultaneous failures.
                        var r = new Random();
                        var wait = r.Next(_retryDelayInSeconds * 2);
                        
                        Logger.Info("Sleeping " + wait + "s after failure");
                        Task.Delay(new TimeSpan(0, 0, 0, wait)).Wait();

                        if (RunChecks)
                        {
                            Logger.Warn(e, "Running checks before we retry");
                            RunDleChecks();
                        }
                    }

                    firstException = firstException ?? e;
                }
            }
            while (remainingRetries-- > 0 && (exitCode == ExitCodeType.Error || exitCode == ExitCodeType.Abort));

            Logger.Info("DLE exited with code " + exitCode);

            switch (exitCode)
            {
                case ExitCodeType.Success:
                case ExitCodeType.OperationNotRequired:
                    {
                        foreach (QueuedImage corrupt in datasetProvider.CorruptMessages)
                            ErrorAndNack(corrupt.Header, corrupt.BasicDeliverEventArgs, "Nacking Corrupt image", null);

                        QueuedImage[] successes = toProcess.Except(datasetProvider.CorruptMessages).ToArray();

                        Ack(successes.Select(x => x.Header).ToList(),
                            successes.Select(x => x.BasicDeliverEventArgs).Max(x => x.DeliveryTag));

                        break;
                    }
                case ExitCodeType.Error:
                case ExitCodeType.Abort:
                    {
                        _stopTokenSource.Cancel();
                        Fatal("DLE Crashed " + (_retryOnFailureCount + 1) + " time(s) on the same batch", firstException);
                        break;
                    }
                default:
                    {
                        _stopTokenSource.Cancel();
                        Fatal("No case for DLE exit code " + exitCode, null);
                        break;
                    }
            }
        }

        private void RunDleChecks()
        {
            var preChecker = new PreExecutionChecker(_lmd, new HICDatabaseConfiguration(_lmd, DatabaseNamer));
            preChecker.Check(new AcceptAllCheckNotifier());
        }

        private int GetQueueCount()
        {
            lock (_oQueueLock)
            {
                return _imageQueue.Count;
            }
        }

        public void Dispose()
        {
            //make sure we stop the consume loop if it hasn't already stopped
            if(_stopTokenSource != null && !_stopTokenSource.IsCancellationRequested)
                _stopTokenSource.Cancel();
        }
    }
}
