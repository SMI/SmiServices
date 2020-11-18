using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using NLog;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;


namespace Microservices.CohortPackager.Execution.ExtractJobStorage
{
    /// <summary>
    /// Base class for any extract job store implementation
    /// </summary>
    public abstract class ExtractJobStore : IExtractJobStore
    {
        protected readonly ILogger Logger;

        protected ExtractJobStore()
        {
            Logger = LogManager.GetLogger(GetType().Name);
        }

        public void PersistMessageToStore(
            [NotNull] ExtractionRequestInfoMessage message,
            [NotNull] IMessageHeader header)
        {
            Logger.Info($"Received new job info {message}");

            // If KeyTag is StudyInstanceUID then ExtractionModality must be specified, otherwise must be null
            if (message.KeyTag == "StudyInstanceUID" ^ !string.IsNullOrWhiteSpace(message.ExtractionModality))
                throw new ApplicationException($"Invalid combination of KeyTag and ExtractionModality (KeyTag={message.KeyTag}, ExtractionModality={message.ExtractionModality}");

            PersistMessageToStoreImpl(message, header);
        }


        public void PersistMessageToStore(ExtractFileCollectionInfoMessage message, IMessageHeader header)
        {
            PersistMessageToStoreImpl(message, header);
        }

        public void PersistMessageToStore(
            [NotNull] ExtractedFileStatusMessage message,
            [NotNull] IMessageHeader header)
        {
            if (message.Status == ExtractedFileStatus.None)
                throw new ApplicationException("ExtractedFileStatus was None");
            if (message.Status == ExtractedFileStatus.Anonymised)
                throw new ApplicationException("Received an anonymisation successful message from the failure queue");

            PersistMessageToStoreImpl(message, header);
        }

        public void PersistMessageToStore(
            [NotNull] ExtractedFileVerificationMessage message,
            [NotNull] IMessageHeader header)
        {
            if (string.IsNullOrWhiteSpace(message.OutputFilePath))
                throw new ApplicationException("Received a verification message without the AnonymisedFileName set");
            if (string.IsNullOrWhiteSpace(message.Report))
                throw new ApplicationException("Null or empty report data");
            if (message.IsIdentifiable && message.Report == "[]")
                throw new ApplicationException("No report data for message marked as identifiable");

            PersistMessageToStoreImpl(message, header);
        }

        public List<ExtractJobInfo> GetReadyJobs(Guid jobId = new Guid())
        {
            Logger.Debug("Getting job info for " + (jobId != Guid.Empty ? jobId.ToString() : "all active jobs"));
            return GetReadyJobsImpl(jobId);
        }

        public void MarkJobCompleted(Guid jobId)
        {
            if (jobId == default(Guid))
                throw new ArgumentNullException(nameof(jobId));

            CompleteJobImpl(jobId);
            Logger.Debug($"Marked job {jobId} as completed");
        }

        public void MarkJobFailed(
            Guid jobId,
            [NotNull] Exception cause)
        {
            if (jobId == default(Guid))
                throw new ArgumentNullException(nameof(jobId));
            if (cause == null)
                throw new ArgumentNullException(nameof(cause));

            MarkJobFailedImpl(jobId, cause);
            Logger.Debug($"Marked job {jobId} as failed");
        }

        public CompletedExtractJobInfo GetCompletedJobInfo(Guid jobId)
        {
            if (jobId == default(Guid))
                throw new ArgumentNullException(nameof(jobId));

            return GetCompletedJobInfoImpl(jobId) ?? throw new ApplicationException("The job store implementation returned a null ExtractJobInfo object");
        }

        public IEnumerable<ExtractionIdentifierRejectionInfo> GetCompletedJobRejections(Guid jobId)
        {
            if (jobId == default(Guid))
                throw new ArgumentNullException(nameof(jobId));

            return GetCompletedJobRejectionsImpl(jobId);
        }

        public IEnumerable<FileAnonFailureInfo> GetCompletedJobAnonymisationFailures(Guid jobId)
        {
            if (jobId == default(Guid))
                throw new ArgumentNullException(nameof(jobId));

            return GetCompletedJobAnonymisationFailuresImpl(jobId);
        }

        public IEnumerable<FileVerificationFailureInfo> GetCompletedJobVerificationFailures(Guid jobId)
        {
            if (jobId == default(Guid))
                throw new ArgumentNullException(nameof(jobId));

            return GetCompletedJobVerificationFailuresImpl(jobId);
        }

        public IEnumerable<string> GetCompletedJobMissingFileList(Guid jobId)
        {
            if (jobId == default)
                throw new ArgumentNullException(nameof(jobId));

            return GetCompletedJobMissingFileListImpl(jobId);
        }

        protected abstract void PersistMessageToStoreImpl(ExtractionRequestInfoMessage message, IMessageHeader header);
        protected abstract void PersistMessageToStoreImpl(ExtractFileCollectionInfoMessage collectionInfoMessage, IMessageHeader header);
        protected abstract void PersistMessageToStoreImpl(ExtractedFileStatusMessage message, IMessageHeader header);
        protected abstract void PersistMessageToStoreImpl(ExtractedFileVerificationMessage message, IMessageHeader header);
        protected abstract List<ExtractJobInfo> GetReadyJobsImpl(Guid specificJobId = new Guid());
        protected abstract void CompleteJobImpl(Guid jobId);
        protected abstract void MarkJobFailedImpl(Guid jobId, Exception e);
        protected abstract CompletedExtractJobInfo GetCompletedJobInfoImpl(Guid jobId);
        protected abstract IEnumerable<ExtractionIdentifierRejectionInfo> GetCompletedJobRejectionsImpl(Guid jobId);
        protected abstract IEnumerable<FileAnonFailureInfo> GetCompletedJobAnonymisationFailuresImpl(Guid jobId);
        protected abstract IEnumerable<FileVerificationFailureInfo> GetCompletedJobVerificationFailuresImpl(Guid jobId);
        protected abstract IEnumerable<string> GetCompletedJobMissingFileListImpl(Guid jobId);
    }
}
