
// ReSharper disable InconsistentlySynchronizedField

using Microservices.CohortPackager.Execution.ExtractJobStorage;
using NLog;
using Smi.Common.Messages.Extraction;
using Smi.Common.Options;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using SysTimers = System.Timers;

namespace Microservices.CohortPackager.Execution.JobProcessing
{
    /// <summary>
    /// Class which does the actual processing of the jobs in the cache
    /// </summary>
    public class ExtractJobWatcher : IExtractJobWatcher
    {
        public int JobsCompleted { get; private set; }

        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        private readonly IFileSystem _fileSystem;
        private readonly FileSystemOptions _fileSystemOptions;

        private readonly IExtractJobStore _jobStore;

        private readonly JobCompleteNotifier _notifier;

        private readonly SysTimers.Timer _processTimer;
        private readonly Action<Exception> _exceptionCallback;
        private readonly object _oProcessorLock = new object();

        private bool _startCalled;


        public ExtractJobWatcher(CohortPackagerOptions options, FileSystemOptions fileSystemOptions, IExtractJobStore jobStore, Action<Exception> exceptionCallback, IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
            _fileSystemOptions = fileSystemOptions;

            _jobStore = jobStore;
            _exceptionCallback = exceptionCallback;
            _notifier = new JobCompleteNotifier(options);

            _processTimer = new SysTimers.Timer(TimeSpan.FromSeconds(options.JobWatcherTimeoutInSeconds).TotalMilliseconds);
            _processTimer.Elapsed += TimerElapsedEvent;
        }


        public void Start()
        {
            _logger.Debug("JobWatcher starting");

            // Do an initial run
            ProcessJobs();

            _processTimer.Start();
            _startCalled = true;
        }

        public void StopProcessing(string reason)
        {
            _logger.Info("Stopping (" + reason + ")");

            _processTimer.Stop();

            // Ensures any currently running process finishes
            lock (_oProcessorLock)
            {
                _logger.Debug("Lock released, no more jobs will be processed");
            }
        }

        public void ProcessJobs(Guid specificJob = new Guid())
        {
            _processTimer.Stop();

            lock (_oProcessorLock)
            {
                List<ExtractJobInfo> jobs = _jobStore.GetLatestJobInfo(specificJob);

                if (jobs.Count == 0)
                {
                    _logger.Info("No jobs to process");
                    return;
                }

                foreach (ExtractJobInfo jobInfo in jobs)
                {
                    try
                    {
                        ProcessJob(jobInfo);
                    }
                    catch (ApplicationException e)
                    {
                        _logger.Warn(e, "Issue with job " + jobInfo.ExtractionJobIdentifier + ", sending to quarantine");

                        //TODO This should also notify that the job has been quarantined
                        _jobStore.QuarantineJob(jobInfo.ExtractionJobIdentifier, e);
                    }
                    catch (Exception e)
                    {
                        StopProcessing("Timed ProcessJob threw an unhandled exception");
                        _exceptionCallback(e);

                        return;
                    }
                }
            }

            // Only restart the timer if it was initially running
            if (_startCalled)
                _processTimer.Start();
        }


        private void TimerElapsedEvent(object source, SysTimers.ElapsedEventArgs ea)
        {
            _logger.Debug("Processing jobs");

            ProcessJobs();
        }

        private void ProcessJob(ExtractJobInfo jobInfo)
        {
            _logger.Debug("Processing job " + jobInfo.ExtractionJobIdentifier);

            if (!jobInfo.JobFileCollectionInfo.Any())
                throw new ApplicationException("The given job info has no file collections set");

            bool doneProcessing = CheckJobFiles(jobInfo);
            if (!doneProcessing)
            {
                _logger.Debug("Some expected files missing for job " + jobInfo.ExtractionJobIdentifier);

                //TODO Check job timeout etc.
                return;
            }

            DoJobCompletionTasks(jobInfo);
        }

        private bool CheckJobFiles(ExtractJobInfo jobInfo)
        {
            _logger.Debug("Checking files for job " + jobInfo.ExtractionJobIdentifier);

            foreach (ExtractFileCollectionInfo item in jobInfo.JobFileCollectionInfo)
            {
                foreach (string filePath in item.ExpectedAnonymisedFiles)
                {
                    if (string.IsNullOrWhiteSpace(filePath))
                        throw new ApplicationException("Expected filePath was null");

                    string absFilePath = _fileSystem.Path.GetFullPath(_fileSystem.Path.Combine(_fileSystemOptions.ExtractRoot, jobInfo.ExtractionDirectory, filePath));
                    _logger.Debug("Scanning for job file  " + absFilePath);

                    // If file exists, continue. Otherwise have to investigate the status messages
                    if (_fileSystem.File.Exists(absFilePath))
                    {
                        _logger.Debug("Found file " + absFilePath);
                        continue;
                    }

                    List<ExtractFileStatusInfo> fileStatuses = jobInfo.JobExtractFileStatuses.Where(x => x.AnonymisedFileName == null).ToList();

                    // No information for the file, have to give up
                    if (!fileStatuses.Any())
                        return false;

                    // Shouldn't be either of these cases if we have got this far
                    if (fileStatuses.Any(x => x.Status == ExtractFileStatus.Anonymised || x.Status == ExtractFileStatus.Unknown))
                        throw new ArgumentException("Have a status message of Anonymised or Unknown for a file we could not locate");

                    // File won't be outputted, will check for this after
                    if (fileStatuses.Any(x => x.Status == ExtractFileStatus.ErrorWontRetry && x.StatusMessage.Contains(filePath)))
                        continue;

                    // Don't think we should ever actually reach here
                    return false;
                }
            }

            return true;
        }

        private void DoJobCompletionTasks(ExtractJobInfo jobInfo)
        {
            _logger.Info("All files for job " + jobInfo.ExtractionJobIdentifier + " present, running completion tasks");

            _jobStore.CleanupJobData(jobInfo.ExtractionJobIdentifier);
            _notifier.NotifyJobCompleted(jobInfo);

            ++JobsCompleted;
        }
    }
}
