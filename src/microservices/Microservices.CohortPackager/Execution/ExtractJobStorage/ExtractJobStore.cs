using System;
using System.Collections.Generic;
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

        public void PersistMessageToStore(ExtractionRequestInfoMessage message, IMessageHeader header)
        {
            Logger.Info($"Received new job info {message}");

            // If KeyTag is StudyInstanceUID then ExtractionModality must be specified, otherwise must be null
            if (message.KeyTag == "StudyInstanceUID" ^ !string.IsNullOrWhiteSpace(message.ExtractionModality))
                throw new ApplicationException($"Invalid combination of KeyTag and ExtractionModality (KeyTag={message.KeyTag}, ExtractionModality={message.ExtractionModality}");

            PersistMessageToStoreImpl(message, header);
        }


        public void PersistMessageToStore(ExtractFileCollectionInfoMessage message, IMessageHeader header)
        {
            Logger.Info($"Received new file collection info {message}");
            PersistMessageToStoreImpl(message, header);
        }

        public void PersistMessageToStore(ExtractFileStatusMessage message, IMessageHeader header)
        {
            if (message.Status == ExtractFileStatus.Anonymised)
                throw new ApplicationException("Received an anonymisation successful message from the failure queue");

            PersistMessageToStoreImpl(message, header);
        }

        public void PersistMessageToStore(IsIdentifiableMessage message, IMessageHeader header)
        {
            if (string.IsNullOrWhiteSpace(message.AnonymisedFileName))
                throw new ApplicationException("Received a verification message without the AnonymisedFileName set");
            if (message.IsIdentifiable ^ !string.IsNullOrWhiteSpace(message.Report))
                throw new ApplicationException($"Invalid combination of IsIdentifiable and Report (KeyTag={message.IsIdentifiable}, ExtractionModality={message.Report}");

            PersistMessageToStoreImpl(message, header);
        }

        public List<ExtractJobInfo> GetReadyJobs(Guid jobId = new Guid())
        {
            Logger.Debug("Getting job info for " + (jobId != Guid.Empty ? jobId.ToString() : "all active jobs"));
            return GetReadyJobsImpl(jobId);
        }

        public void MarkJobCompleted(Guid jobId)
        {
            Logger.Debug($"Marking job {jobId} as completed");
            CompleteJobImpl(jobId);
        }

        public void MarkJobFailed(Guid jobId, Exception e)
        {
            Logger.Debug($"Marking job {jobId} as failed");
            MarkJobFailedImpl(jobId, e);
        }

        protected abstract void PersistMessageToStoreImpl(ExtractionRequestInfoMessage message, IMessageHeader header);
        protected abstract void PersistMessageToStoreImpl(ExtractFileCollectionInfoMessage collectionInfoMessage, IMessageHeader header);
        protected abstract void PersistMessageToStoreImpl(ExtractFileStatusMessage message, IMessageHeader header);
        protected abstract void PersistMessageToStoreImpl(IsIdentifiableMessage message, IMessageHeader header);
        protected abstract List<ExtractJobInfo> GetReadyJobsImpl(Guid specificJobId = new Guid());
        protected abstract void CompleteJobImpl(Guid jobId);
        protected abstract void MarkJobFailedImpl(Guid jobId, Exception e);
    }
}
