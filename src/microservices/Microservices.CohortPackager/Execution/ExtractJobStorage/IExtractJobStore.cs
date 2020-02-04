
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using System;
using System.Collections.Generic;

namespace Microservices.CohortPackager.Execution.ExtractJobStorage
{
    /// <summary>
    /// Interface for objects which represent a store of extract job information
    /// </summary>
    public interface IExtractJobStore
    {
        /// <summary>
        /// Serializes a <see cref="ExtractionRequestInfoMessage"/> and it's <see cref="IMessageHeader"/> and stores it
        /// </summary>
        /// <param name="requestInfoMessage"></param>
        /// <param name="header"></param>
        void PersistMessageToStore(ExtractionRequestInfoMessage requestInfoMessage, IMessageHeader header);

        /// <summary>
        /// Serializes a <see cref="ExtractFileCollectionInfoMessage"/> and it's <see cref="IMessageHeader"/> and stores it
        /// </summary>
        /// <param name="collectionInfoMessage"></param>
        /// <param name="header"></param>
        void PersistMessageToStore(ExtractFileCollectionInfoMessage collectionInfoMessage, IMessageHeader header);

        /// <summary>
        /// Serializes a <see cref="ExtractFileStatusMessage"/> and it's <see cref="IMessageHeader"/> and stores it
        /// </summary>
        /// <param name="fileStatusMessage"></param>
        /// <param name="header"></param>
        void PersistMessageToStore(ExtractFileStatusMessage fileStatusMessage, IMessageHeader header);

        /// <summary>
        /// Serializes a <see cref="ExtractFileStatusMessage"/> and it's <see cref="IMessageHeader"/> and stores it
        /// </summary>
        /// <param name="anonVerificationMessage"></param>
        /// <param name="header"></param>
        void PersistMessageToStore(IsIdentifiableMessage anonVerificationMessage, IMessageHeader header);

        /// <summary>
        /// Returns a list of all jobs which are in progress (where status is <see cref="ExtractJobStatus.WaitingForFiles"/>)
        /// </summary>
        /// <param name="extractionJobIdentifier">A specific job to get <see cref="ExtractJobInfo"/> for. Empty returns all jobs in progress</param>
        /// <returns></returns>
        List<ExtractJobInfo> GetLatestJobInfo(Guid extractionJobIdentifier = new Guid());

        /// <summary>
        /// Cleanup/archive any data in the database related to an extract job
        /// </summary>
        /// <param name="extractionJobIdentifier"></param>
        void CleanupJobData(Guid extractionJobIdentifier);

        /// <summary>
        /// Quarantines a job if there is some issue processing it
        /// </summary>
        /// <param name="extractionJobIdentifier"></param>
        /// <param name="e"></param>
        void QuarantineJob(Guid extractionJobIdentifier, Exception e);
    }
}
