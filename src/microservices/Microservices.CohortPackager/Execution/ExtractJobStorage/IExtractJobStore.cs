
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using System;
using System.Collections.Concurrent;
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
        void PersistMessageToStore(ExtractedFileVerificationMessage anonVerificationMessage, IMessageHeader header);

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
        CompletedExtractJobInfo GetCompletedJobInfo(Guid jobId);

        /// <summary>
        /// Returns the rejection reasons for a completed job
        /// </summary>
        /// <param name="jobId"></param>
        /// <returns></returns>
        IEnumerable<ExtractionIdentifierRejectionInfo> GetCompletedJobRejections(Guid jobId);

        /// <summary>
        /// Returns the anonymisation failures for a completed job
        /// </summary>
        /// <param name="jobId"></param>
        /// <returns></returns>
        IEnumerable<FileAnonFailureInfo> GetCompletedJobAnonymisationFailures(Guid jobId);

        /// <summary>
        /// Returns the verification failures for a completed job
        /// </summary>
        /// <param name="jobId"></param>
        /// <returns></returns>
        IEnumerable<FileVerificationFailureInfo> GetCompletedJobVerificationFailures(Guid jobId);

        /// <summary>
        /// Returns the full list of files that were matched from an input identifier but could not be found, and a reason for each
        /// </summary>
        /// <param name="jobId"></param>
        /// <returns></returns>
        IEnumerable<string> GetCompletedJobMissingFileList(Guid jobId);

        /// <summary>
        /// Add a <see cref="ExtractedFileVerificationMessage"/> to the write queue. The message should not be acknowledged
        /// until the corresponding tag is reutrned by <see cref="ProcessedVerificationMessages"/>
        /// </summary>
        /// <param name="message"></param>
        /// <param name="header"></param>
        /// <param name="tag"></param>
        void AddToWriteQueue(ExtractedFileVerificationMessage message, IMessageHeader header, ulong tag);

        /// <summary>
        /// Process all queued <see cref="ExtractedFileVerificationMessage"/>s into the store
        /// </summary>
        void ProcessVerificationMessageQueue();

        /// <summary>
        /// All processed status messages which can be acknowledged
        /// </summary>
        ConcurrentQueue<Tuple<IMessageHeader, ulong>> ProcessedVerificationMessages { get; }
    }
}
