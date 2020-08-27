
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
        /// Serializes a <see cref="ExtractedFileStatusMessage"/> and it's <see cref="IMessageHeader"/> and stores it
        /// </summary>
        /// <param name="fileStatusMessage"></param>
        /// <param name="header"></param>
        void PersistMessageToStore(ExtractedFileStatusMessage fileStatusMessage, IMessageHeader header);

        /// <summary>
        /// Serializes a <see cref="ExtractedFileStatusMessage"/> and it's <see cref="IMessageHeader"/> and stores it
        /// </summary>
        /// <param name="anonVerificationMessage"></param>
        /// <param name="header"></param>
        void PersistMessageToStore(IsIdentifiableMessage anonVerificationMessage, IMessageHeader header);

        /// <summary>
        /// Returns a list of all jobs which are ready for final checks
        /// </summary>
        /// <param name="extractionJobIdentifier">A specific job to get <see cref="ExtractJobInfo"/> for. Empty returns all jobs in progress</param>
        /// <returns></returns>
        List<ExtractJobInfo> GetReadyJobs(Guid extractionJobIdentifier = new Guid());

        /// <summary>
        /// Cleanup/archive any data in the database related to an extract job
        /// </summary>
        /// <param name="extractionJobIdentifier"></param>
        void MarkJobCompleted(Guid extractionJobIdentifier);

        /// <summary>
        /// Quarantines a job if there is some issue processing it
        /// </summary>
        /// <param name="extractionJobIdentifier"></param>
        /// <param name="cause"></param>
        void MarkJobFailed(Guid extractionJobIdentifier, Exception cause);

        /// <summary>
        /// Returns the ExtractJobInfo for a completed job
        /// </summary>
        /// <param name="jobId"></param>
        /// <returns></returns>
        ExtractJobInfo GetCompletedJobInfo(Guid jobId);

        /// <summary>
        /// Returns the rejection reasons for a completed job
        /// </summary>
        /// <param name="jobId"></param>
        /// <returns></returns>
        IEnumerable<Tuple<string, Dictionary<string, int>>> GetCompletedJobRejections(Guid jobId);

        /// <summary>
        /// Returns the anonymisation failures for a completed job. This is a tuple of "expected anonymised file path" and "failure reason"
        /// </summary>
        /// <param name="jobId"></param>
        /// <returns></returns>
        IEnumerable<Tuple<string, string>> GetCompletedJobAnonymisationFailures(Guid jobId);

        /// <summary>
        /// Returns the verification failures for a completed job. This is a tuple of "anonymised file path" and "verification failure reason"
        /// </summary>
        /// <param name="jobId"></param>
        /// <returns></returns>
        IEnumerable<Tuple<string, string>> GetCompletedJobVerificationFailures(Guid jobId);
    }
}
