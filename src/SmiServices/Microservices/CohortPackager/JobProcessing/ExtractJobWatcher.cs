using NLog;
using SmiServices.Common.Options;
using SmiServices.Microservices.CohortPackager.ExtractJobStorage;
using SmiServices.Microservices.CohortPackager.JobProcessing.Notifying;
using SmiServices.Microservices.CohortPackager.JobProcessing.Reporting;
using System;
using System.Collections.Generic;
using SysTimers = System.Timers;

namespace SmiServices.Microservices.CohortPackager.JobProcessing
{
    /// <summary>
    /// Class which periodically queries the job store for any ready jobs and performs any final checks
    /// </summary>
    public class ExtractJobWatcher : IExtractJobWatcher
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        private readonly IExtractJobStore _jobStore;

        private readonly IJobReporter _reporter;
        private readonly IJobCompleteNotifier _notifier;

        private readonly SysTimers.Timer _processTimer;
        private readonly Action<Exception> _exceptionCallback;
        private readonly object _oProcessorLock = new();

        private bool _startCalled;


        public ExtractJobWatcher(
            CohortPackagerOptions options,
            IExtractJobStore jobStore,
            Action<Exception> exceptionCallback,
            IJobCompleteNotifier jobCompleteNotifier,
            IJobReporter reporter)
        {
            _jobStore = jobStore;
            _exceptionCallback = exceptionCallback;

            _reporter = reporter;
            _notifier = jobCompleteNotifier;

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
            _logger.Info($"Stopping ({reason})");

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
                List<ExtractJobInfo> jobs = _jobStore.GetReadyJobs(specificJob);

                if (jobs.Count == 0)
                    _logger.Debug("No jobs ready for checks");

                foreach (ExtractJobInfo job in jobs)
                {
                    Guid jobId = job.ExtractionJobIdentifier;

                    try
                    {
                        DoJobCompletionTasks(job);
                    }
                    catch (ApplicationException e)
                    {
                        _logger.Warn(e, $"Issue with job {jobId}, marking as failed");
                        _jobStore.MarkJobFailed(jobId, e);
                    }
                    catch (Exception e)
                    {
                        StopProcessing("ProcessJob threw an unhandled exception");
                        _exceptionCallback(e);
                        return;
                    }
                }
            }

            // Only restart the timer if it was initially running
            if (_startCalled)
                _processTimer.Start();
        }

        private void TimerElapsedEvent(object? source, SysTimers.ElapsedEventArgs ea)
        {
            _logger.Debug("Checking job statuses");
            ProcessJobs();
        }

        private void DoJobCompletionTasks(ExtractJobInfo jobInfo)
        {
            Guid jobId = jobInfo.ExtractionJobIdentifier;

            if (jobInfo.JobStatus != ExtractJobStatus.ReadyForChecks)
                throw new ApplicationException($"Job {jobId} is not ready for checks");

            _logger.Info($"All files for job {jobId} present, running completion tasks");

            _jobStore.MarkJobCompleted(jobId);

            _reporter.CreateReports(jobId);
            _logger.Info($"Report for {jobId} created");

            _notifier.NotifyJobCompleted(jobInfo);
        }
    }
}
