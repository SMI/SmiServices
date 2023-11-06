using Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB.ObjectModel;
using MongoDB.Driver;
using Smi.Common.Helpers;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB
{
    // ReSharper disable InconsistentlySynchronizedField
    public class MongoExtractJobStore : ExtractJobStore
    {
        private const string ExtractJobCollectionName = "inProgressJobs";
        private const string ExpectedFilesCollectionPrefix = "expectedFiles";
        private const string StatusCollectionPrefix = "statuses";
        private const string CompletedCollectionName = "completedJobs";

        private readonly IMongoClient _client;
        private readonly IMongoDatabase _database;

        // NOTE(rkm 2020-03-08) The collections listed here are persistent. Job-specific collections for the files info and statuses are temporary and fetched when needed
        private readonly IMongoCollection<MongoExtractJobDoc> _inProgressJobCollection;
        private readonly IMongoCollection<MongoExpectedFilesDoc> _completedExpectedFilesCollection;
        private readonly IMongoCollection<MongoFileStatusDoc> _completedStatusCollection;
        private readonly IMongoCollection<MongoCompletedExtractJobDoc> _completedJobCollection;

        private readonly DateTimeProvider _dateTimeProvider;


        public MongoExtractJobStore(IMongoClient client, string extractionDatabaseName, DateTimeProvider? dateTimeProvider = null)
        {
            _client = client;
            _database = _client.GetDatabase(extractionDatabaseName);

            _dateTimeProvider = dateTimeProvider ?? new DateTimeProvider();

            _inProgressJobCollection = _database.GetCollection<MongoExtractJobDoc>(ExtractJobCollectionName);
            _completedExpectedFilesCollection = _database.GetCollection<MongoExpectedFilesDoc>(ExpectedFilesCollectionName("completed"));
            _completedStatusCollection = _database.GetCollection<MongoFileStatusDoc>(StatusCollectionName("completed"));
            _completedJobCollection = _database.GetCollection<MongoCompletedExtractJobDoc>(CompletedCollectionName);

            long count = _inProgressJobCollection.CountDocuments(FilterDefinition<MongoExtractJobDoc>.Empty);
            Logger.Info(count > 0 ? $"Connected to job store with {count} existing jobs" : "Empty job store created successfully");
        }

        protected override void PersistMessageToStoreImpl(ExtractionRequestInfoMessage message, IMessageHeader header)
        {
            if (InCompletedJobCollection(message.ExtractionJobIdentifier))
                throw new ApplicationException("Received an ExtractionRequestInfoMessage for a job that is already completed");

            MongoExtractJobDoc newJobInfo = MongoExtractJobDoc.FromMessage(message, header, _dateTimeProvider);

            _inProgressJobCollection.InsertOne(newJobInfo);
        }

        protected override void PersistMessageToStoreImpl(ExtractFileCollectionInfoMessage message, IMessageHeader header)
        {
            if (InCompletedJobCollection(message.ExtractionJobIdentifier))
                throw new ApplicationException("Received an ExtractFileCollectionInfoMessage for a job that is already completed");

            MongoExpectedFilesDoc expectedFilesForKey = MongoExpectedFilesDoc.FromMessage(message, header, _dateTimeProvider);

            _database
                .GetCollection<MongoExpectedFilesDoc>(ExpectedFilesCollectionName(message.ExtractionJobIdentifier))
                .InsertOne(expectedFilesForKey);
        }

        protected override void PersistMessageToStoreImpl(ExtractedFileStatusMessage message, IMessageHeader header)
        {
            if (InCompletedJobCollection(message.ExtractionJobIdentifier))
                throw new ApplicationException("Received an ExtractedFileStatusMessage for a job that is already completed");

            var newStatus = new MongoFileStatusDoc(
                MongoExtractionMessageHeaderDoc.FromMessageHeader(message.ExtractionJobIdentifier, header, _dateTimeProvider),
                message.DicomFilePath,
                message.OutputFilePath,
                message.Status,
                VerifiedFileStatus.NotVerified,
                statusMessage: message.StatusMessage
            );

            _database
                .GetCollection<MongoFileStatusDoc>(StatusCollectionName(message.ExtractionJobIdentifier))
                .InsertOne(newStatus);
        }

        protected override void PersistMessageToStoreImpl(ExtractedFileVerificationMessage message, IMessageHeader header)
        {
            if (InCompletedJobCollection(message.ExtractionJobIdentifier))
                throw new ApplicationException("Received an ExtractedFileVerificationMessage for a job that is already completed");

            var newStatus = new MongoFileStatusDoc(
                MongoExtractionMessageHeaderDoc.FromMessageHeader(message.ExtractionJobIdentifier, header, _dateTimeProvider),
                message.DicomFilePath,
                message.OutputFilePath,
                ExtractedFileStatus.Anonymised,
                message.Status,
                statusMessage: message.Report
            );

            _database
                .GetCollection<MongoFileStatusDoc>(StatusCollectionName(message.ExtractionJobIdentifier))
                .InsertOne(newStatus);
        }

        //TODO(rkm 2020-03-09) Test this with a large volume of messages
        protected override List<ExtractJobInfo> GetReadyJobsImpl(Guid specificJobId = default(Guid))
        {
            //TODO Docs

            FilterDefinition<MongoExtractJobDoc> filter = FilterDefinition<MongoExtractJobDoc>.Empty;

            // If we have been passed a specific GUID, search for that job only
            if (specificJobId != default(Guid))
                filter &= Builders<MongoExtractJobDoc>.Filter.Eq(x => x.ExtractionJobIdentifier, specificJobId);

            // NOTE(rkm 2020-03-03) Get all extract jobs that are not in the Failed state
            var activeJobs = new List<MongoExtractJobDoc>();
            using (IAsyncCursor<MongoExtractJobDoc> cursor = _inProgressJobCollection.FindSync(filter))
            {
                while (cursor.MoveNext())
                    foreach (MongoExtractJobDoc job in cursor.Current)
                    {
                        if (job.JobStatus == ExtractJobStatus.Failed)
                        {
                            Logger.Warn($"Job {job.ExtractionJobIdentifier} is marked as Failed - ignoring");
                            continue;
                        }
                        activeJobs.Add(job);
                    }
            }

            // Calculate the current status of each job and return those that are ready for completion
            var readyJobs = new List<ExtractJobInfo>();
            foreach (MongoExtractJobDoc job in activeJobs)
            {
                Guid jobId = job.ExtractionJobIdentifier;

                Logger.Debug($"Checking progress for {jobId}");

                // Check if the job has progressed
                var changed = false;

                string expectedTempCollectionName = ExpectedFilesCollectionName(jobId);
                IMongoCollection<MongoExpectedFilesDoc> expectedTempCollection = _database.GetCollection<MongoExpectedFilesDoc>(expectedTempCollectionName);

                if (job.JobStatus == ExtractJobStatus.WaitingForCollectionInfo)
                {
                    long collectionInfoCount = expectedTempCollection.CountDocuments(FilterDefinition<MongoExpectedFilesDoc>.Empty);

                    if (job.KeyCount == collectionInfoCount)
                    {
                        Logger.Debug($"Have all collection messages for job {jobId}");
                        job.JobStatus = ExtractJobStatus.WaitingForStatuses;
                        changed = true;
                    }
                    else
                        Logger.Debug($"Job {jobId} is in state WaitingForCollectionInfo. Expected count is {job.KeyCount}, actual is {collectionInfoCount}");
                }

                if (job.JobStatus == ExtractJobStatus.WaitingForStatuses)
                {
                    string statusTempCollectionName = StatusCollectionName(jobId);
                    IMongoCollection<MongoFileStatusDoc> statusTempCollection = _database.GetCollection<MongoFileStatusDoc>(statusTempCollectionName);

                    // If we have (at least) one status message for each expected file, then we can continue
                    var expectedStatusesCount = 0;
                    IAsyncCursor<MongoExpectedFilesDoc> cursor = expectedTempCollection.FindSync(FilterDefinition<MongoExpectedFilesDoc>.Empty);
                    while (cursor.MoveNext())
                        expectedStatusesCount += cursor.Current.Sum(doc => doc.ExpectedFiles.Count);

                    long actualStatusCount = statusTempCollection.CountDocuments(FilterDefinition<MongoFileStatusDoc>.Empty);

                    if (expectedStatusesCount == actualStatusCount)
                    {
                        Logger.Debug($"Have all status messages for job {jobId}");
                        job.JobStatus = ExtractJobStatus.ReadyForChecks;
                        changed = true;
                    }
                    else
                        Logger.Debug($"Job {jobId} is in state WaitingForStatuses. Expected count is {expectedStatusesCount}, actual is {actualStatusCount}");
                }

                // If the status has moved on then update the document in the database
                if (changed)
                {
                    ReplaceOneResult res = _inProgressJobCollection.ReplaceOne(GetFilterForSpecificJob<MongoExtractJobDoc>(jobId), job);
                    if (!res.IsAcknowledged)
                        throw new ApplicationException($"Received invalid ReplaceOneResult: {res}");
                }

                if (job.JobStatus != ExtractJobStatus.ReadyForChecks)
                {
                    Logger.Debug($"Job {jobId} is not ready - currently in state {job.JobStatus} (changed={changed})");
                    continue;
                }

                readyJobs.Add(job.ToExtractJobInfo());
            }

            return readyJobs;
        }

        protected override void CompleteJobImpl(Guid jobId)
        {
            //TODO Docs

            using IClientSessionHandle session = _client.StartSession();
            session.StartTransaction();
            string expectedCollNameForJob = ExpectedFilesCollectionName(jobId);
            string statusCollNameForJob = StatusCollectionName(jobId);

            try
            {
                if (!TryGetMongoExtractJobDoc(jobId, out MongoExtractJobDoc toComplete))
                    throw new ApplicationException($"Could not find job {jobId} in the job store");

                if (toComplete.JobStatus == ExtractJobStatus.Failed)
                    throw new ApplicationException($"Job {jobId} is marked as failed");

                var completedJob = new MongoCompletedExtractJobDoc(toComplete, _dateTimeProvider.UtcNow());
                _completedJobCollection.InsertOne(completedJob);

                DeleteResult res = _inProgressJobCollection.DeleteOne(GetFilterForSpecificJob<MongoExtractJobDoc>(jobId));
                if (!res.IsAcknowledged)
                    throw new ApplicationException("Job data was archived but could not delete original from job store");

                // Move the associated docs from each collection to the archives

                IMongoCollection<MongoExpectedFilesDoc> expectedTempCollection = _database.GetCollection<MongoExpectedFilesDoc>(expectedCollNameForJob);
                if (expectedTempCollection.CountDocuments(FilterDefinition<MongoExpectedFilesDoc>.Empty) == 0)
                    throw new ApplicationException($"Collection of MongoExpectedFilesDoc for job {jobId} was missing or empty");
                using (IAsyncCursor<MongoExpectedFilesDoc> cursor = expectedTempCollection.FindSync(FilterDefinition<MongoExpectedFilesDoc>.Empty))
                {
                    while (cursor.MoveNext())
                        _completedExpectedFilesCollection.InsertMany(cursor.Current);
                }

                IMongoCollection<MongoFileStatusDoc> statusTemp = _database.GetCollection<MongoFileStatusDoc>(statusCollNameForJob);
                if (statusTemp.CountDocuments(FilterDefinition<MongoFileStatusDoc>.Empty) == 0)
                    throw new ApplicationException($"Collection of MongoFileStatusDoc for job {jobId} was missing or empty");
                using (IAsyncCursor<MongoFileStatusDoc> cursor = statusTemp.FindSync(FilterDefinition<MongoFileStatusDoc>.Empty))
                {
                    while (cursor.MoveNext())
                        _completedStatusCollection.InsertMany(cursor.Current);
                }
            }
            catch (Exception)
            {
                Logger.Debug("Caught exception from transaction. Aborting before re-throwing");
                session.AbortTransaction();
                throw;
            }

            // TODO(rkm 2020-03-03) Can potentially add a retry here
            session.CommitTransaction();

            // NOTE(rkm 2020-03-09) "Operations that affect the database catalog, such as creating or dropping a collection or an index, are not allowed in transactions"
            _database.DropCollection(expectedCollNameForJob);
            _database.DropCollection(statusCollNameForJob);
        }

        protected override void MarkJobFailedImpl(Guid jobId, Exception cause)
        {
            //TODO Docs

            using IClientSessionHandle session = _client.StartSession();
            session.StartTransaction();

            try
            {
                if (!TryGetMongoExtractJobDoc(jobId, out MongoExtractJobDoc toFail))
                    throw new ApplicationException($"Could not find job {jobId} in the job store");

                if (toFail.JobStatus == ExtractJobStatus.Failed || toFail.FailedJobInfoDoc != null)
                    throw new ApplicationException($"Job {jobId} is already marked as failed");

                toFail.JobStatus = ExtractJobStatus.Failed;
                toFail.FailedJobInfoDoc = new MongoFailedJobInfoDoc(cause, _dateTimeProvider);

                ReplaceOneResult res = _inProgressJobCollection.ReplaceOne(GetFilterForSpecificJob<MongoExtractJobDoc>(jobId), toFail);
                if (!res.IsAcknowledged || res.ModifiedCount != 1)
                    throw new ApplicationException($"Received invalid ReplaceOneResult: {res}");
            }
            catch (Exception)
            {
                Logger.Debug("Caught exception from transaction. Aborting before re-throwing");
                session.AbortTransaction();
                throw;
            }

            // TODO(rkm 2020-03-03) Can potentially add a retry here
            session.CommitTransaction();
        }

        protected override CompletedExtractJobInfo GetCompletedJobInfoImpl(Guid jobId)
        {
            MongoCompletedExtractJobDoc jobDoc =
                _completedJobCollection
                .FindSync(Builders<MongoCompletedExtractJobDoc>.Filter.Eq(x => x.ExtractionJobIdentifier, jobId))
                .SingleOrDefault();

            if (jobDoc == null)
                throw new ApplicationException($"No completed document for job {jobId}");

            return jobDoc.ToExtractJobInfo();
        }

        protected override IEnumerable<ExtractionIdentifierRejectionInfo> GetCompletedJobRejectionsImpl(Guid jobId)
        {
            var filter = FilterDefinition<MongoExpectedFilesDoc>.Empty;
            filter &= Builders<MongoExpectedFilesDoc>.Filter.Eq(x => x.Header.ExtractionJobIdentifier, jobId);
            // TODO(rkm 2020-10-28) This doesn't work for some reason, so for now we're using the check inside the foreach loop
            //filter &= Builders<MongoExpectedFilesDoc>.Filter.Gt(x => x.RejectedKeys.RejectionInfo.Count, 0);
            IAsyncCursor<MongoExpectedFilesDoc> cursor = _completedExpectedFilesCollection.FindSync(filter);
            while (cursor.MoveNext())
                foreach (MongoExpectedFilesDoc expectedFilesDoc in cursor.Current)
                {
                    if (expectedFilesDoc.RejectedKeys.RejectionInfo.Count == 0)
                        continue;
                    yield return new ExtractionIdentifierRejectionInfo(expectedFilesDoc.Key, expectedFilesDoc.RejectedKeys.RejectionInfo);
                }
        }

        protected override IEnumerable<FileAnonFailureInfo> GetCompletedJobAnonymisationFailuresImpl(Guid jobId)
        {
            var filter = FilterDefinition<MongoFileStatusDoc>.Empty;
            filter &= Builders<MongoFileStatusDoc>.Filter.Eq(x => x.Header.ExtractionJobIdentifier, jobId);
            filter &= Builders<MongoFileStatusDoc>.Filter.Or(
                Builders<MongoFileStatusDoc>.Filter.Eq(x => x.ExtractedFileStatus, ExtractedFileStatus.Anonymised),
                Builders<MongoFileStatusDoc>.Filter.Eq(x => x.ExtractedFileStatus, ExtractedFileStatus.Copied)
            );
            filter &= Builders<MongoFileStatusDoc>.Filter.Eq(x => x.VerifiedFileStatus, VerifiedFileStatus.NotVerified);
            return CompletedStatusDocsForFilter(filter).Select(x => new FileAnonFailureInfo(x.Item1, x.Item2));
        }

        protected override IEnumerable<FileVerificationFailureInfo> GetCompletedJobVerificationFailuresImpl(Guid jobId)
        {
            var filter = FilterDefinition<MongoFileStatusDoc>.Empty;
            filter &= Builders<MongoFileStatusDoc>.Filter.Eq(x => x.Header.ExtractionJobIdentifier, jobId);
            filter &= Builders<MongoFileStatusDoc>.Filter.Or(
                 Builders<MongoFileStatusDoc>.Filter.Ne(x => x.ExtractedFileStatus, ExtractedFileStatus.Anonymised),
                 Builders<MongoFileStatusDoc>.Filter.Ne(x => x.ExtractedFileStatus, ExtractedFileStatus.Copied)
             );
            filter &= Builders<MongoFileStatusDoc>.Filter.Eq(x => x.VerifiedFileStatus, VerifiedFileStatus.IsIdentifiable);
            return CompletedStatusDocsForFilter(filter).Select(x => new FileVerificationFailureInfo(x.Item1, x.Item2));
        }

        protected override IEnumerable<string> GetCompletedJobMissingFileListImpl(Guid jobId)
        {
            FilterDefinition<MongoFileStatusDoc> filter = FilterDefinition<MongoFileStatusDoc>.Empty;
            filter &= Builders<MongoFileStatusDoc>.Filter.Eq(x => x.Header.ExtractionJobIdentifier, jobId);
            filter &= Builders<MongoFileStatusDoc>.Filter.Eq(x => x.ExtractedFileStatus, ExtractedFileStatus.FileMissing);
            IAsyncCursor<MongoFileStatusDoc> cursor = _completedStatusCollection.FindSync(filter);
            while (cursor.MoveNext())
                foreach (MongoFileStatusDoc doc in cursor.Current)
                    yield return doc.DicomFilePath;
        }

        #region Helper Methods

        private static string StatusCollectionName(string name) => $"{StatusCollectionPrefix}_{name}";
        private static string StatusCollectionName(Guid jobId) => StatusCollectionName(jobId.ToString());
        private static string ExpectedFilesCollectionName(string name) => $"{ExpectedFilesCollectionPrefix}_{name}";
        private static string ExpectedFilesCollectionName(Guid jobId) => ExpectedFilesCollectionName(jobId.ToString());

        private static FilterDefinition<T> GetFilterForSpecificJob<T>(Guid extractionJobIdentifier) where T : MongoExtractJobDoc
            => Builders<T>.Filter.Eq(x => x.ExtractionJobIdentifier, extractionJobIdentifier);

        private bool TryGetMongoExtractJobDoc(Guid extractionJobIdentifier, out MongoExtractJobDoc mongoExtractJobDoc)
        {
            mongoExtractJobDoc = _inProgressJobCollection
                .Find(GetFilterForSpecificJob<MongoExtractJobDoc>(extractionJobIdentifier))
                .SingleOrDefault();
            return mongoExtractJobDoc != null;
        }

        private bool InCompletedJobCollection(Guid extractionJobIdentifier)
        {
            return _completedJobCollection
                .Find(GetFilterForSpecificJob<MongoCompletedExtractJobDoc>(extractionJobIdentifier))
                .SingleOrDefault() != null;
        }

        private IEnumerable<Tuple<string, string>> CompletedStatusDocsForFilter(FilterDefinition<MongoFileStatusDoc> filter)
        {
            IAsyncCursor<MongoFileStatusDoc> cursor = _completedStatusCollection.FindSync(filter);
            while (cursor.MoveNext())
                foreach (MongoFileStatusDoc doc in cursor.Current)
                    yield return new Tuple<string, string>(doc.OutputFileName!, doc.StatusMessage!);
        }

        #endregion
    }
}
