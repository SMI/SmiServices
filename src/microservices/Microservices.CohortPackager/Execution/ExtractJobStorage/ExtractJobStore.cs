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
                throw new ApplicationException("Invalid combination of KeyTag and ExtractionModality");

            PersistMessageToStoreImpl(message, header);
        }


        public void PersistMessageToStore(ExtractFileCollectionInfoMessage message, IMessageHeader header)
        {
            Logger.Info($"Received new file collection info {message}");

            // TODO(rkm 2020-02-04) Handle message.RejectionReasons
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
            PersistMessageToStoreImpl(message, header);
        }

        public List<ExtractJobInfo> GetLatestJobInfo(Guid jobId = new Guid())
        {
            Logger.Debug("Getting job info for " + (jobId != Guid.Empty ? jobId.ToString() : "all active jobs"));
            return GetLatestJobInfoImpl(jobId);
        }

        public void CleanupJobData(Guid jobId)
        {
            Logger.Debug($"Cleaning up job data for {jobId}");
            CleanupJobDataImpl(jobId);
        }

        public void QuarantineJob(Guid jobId, Exception e)
        {
            Logger.Debug($"Quarantining job data for {jobId}");
            QuarantineJobImpl(jobId, e);
        }

        protected abstract void PersistMessageToStoreImpl(ExtractionRequestInfoMessage message, IMessageHeader header);
        protected abstract void PersistMessageToStoreImpl(ExtractFileCollectionInfoMessage collectionInfoMessage, IMessageHeader header);
        protected abstract void PersistMessageToStoreImpl(ExtractFileStatusMessage message, IMessageHeader header);
        protected abstract void PersistMessageToStoreImpl(IsIdentifiableMessage message, IMessageHeader header);
        protected abstract List<ExtractJobInfo> GetLatestJobInfoImpl(Guid jobId = new Guid());
        protected abstract void CleanupJobDataImpl(Guid jobId);
        protected abstract void QuarantineJobImpl(Guid jobId, Exception e);
    }
}
