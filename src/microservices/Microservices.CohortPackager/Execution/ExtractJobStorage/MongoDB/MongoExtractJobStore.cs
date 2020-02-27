using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB.ObjectModel;
using MongoDB.Driver;
using Smi.Common.Helpers;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;


namespace Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB
{
    public class MongoExtractJobStore : ExtractJobStore
    {
        private const string ExtractJobCollectionName = "extractJobStore";
        private const string QuarantineCollectionName = "extractJobQuarantine";
        private const string ArchiveCollectionName = "extractJobArchive";
        private const string StatusCollectionPrefix = "temp_statuses_";

        private readonly IMongoDatabase _database;

        // NOTE(rkm 2020-02-25) Lock needed when accessing any of the below collections, but not the status collections
        // TODO(rkm 2020-02-06) Make this transactional and remove the lock
        private readonly object _oJobStoreLock = new object();
        private readonly IMongoCollection<MongoExtractJob> _jobInfoCollection;
        private readonly IMongoCollection<ArchivedMongoExtractJob> _jobArchiveCollection;
        private readonly IMongoCollection<QuarantinedMongoExtractJob> _jobQuarantineCollection;

        private readonly FindOptions<MongoExtractJob> _findOptions = new FindOptions<MongoExtractJob>
        {
            BatchSize = 1,
            NoCursorTimeout = false
        };

        private readonly DateTimeProvider _dateTimeProvider;

        public MongoExtractJobStore(IMongoDatabase database)
            : this(database, new DateTimeProvider()) { }

        public MongoExtractJobStore(IMongoDatabase database, DateTimeProvider dateTimeProvider)
        {
            _database = database;
            _dateTimeProvider = dateTimeProvider;

            _jobInfoCollection = _database.GetCollection<MongoExtractJob>(ExtractJobCollectionName);
            _jobArchiveCollection = _database.GetCollection<ArchivedMongoExtractJob>(ArchiveCollectionName);
            _jobQuarantineCollection = _database.GetCollection<QuarantinedMongoExtractJob>(QuarantineCollectionName);

            long count = _jobInfoCollection.CountDocuments(FilterDefinition<MongoExtractJob>.Empty);

            Logger.Info(count > 0 ? $"Connected to job store with {count} existing jobs" : "Empty job store created successfully");
        }

        protected override void PersistMessageToStoreImpl(ExtractionRequestInfoMessage requestInfoMessage, IMessageHeader header)
        {
            Guid jobIdentifier = requestInfoMessage.ExtractionJobIdentifier;
            MongoExtractJobHeader jobHeader = MongoExtractJobHeader.FromMessageHeader(header, _dateTimeProvider);

            lock (_oJobStoreLock)
            {
                if (InArchiveCollection(jobIdentifier, out MongoExtractJob _) || InQuarantineCollection(jobIdentifier, out _))
                    throw new ApplicationException(
                        "Received an ExtractionRequestInfoMessage for a job that exists in the archive or quarantine");

                if (InJobCollection(jobIdentifier, out MongoExtractJob existing))
                {
                    if (existing.JobStatus != ExtractJobStatus.WaitingForJobInfo)
                        throw new ApplicationException(
                            "Received ExtractionRequestInfoMessage for a job which has already started processing");

                    existing.Header = jobHeader;
                    existing.ProjectNumber = requestInfoMessage.ProjectNumber;
                    existing.ExtractionDirectory = requestInfoMessage.ExtractionDirectory;
                    existing.KeyCount = requestInfoMessage.KeyValueCount;
                    existing.ExtractionModality = requestInfoMessage.ExtractionModality;
                    existing.JobSubmittedAt = requestInfoMessage.JobSubmittedAt;

                    if (existing.KeyCount == existing.ExpectedFilesInfo.Count)
                        existing.JobStatus = ExtractJobStatus.WaitingForStatuses;

                    ReplaceOneResult res = _jobInfoCollection.ReplaceOne(GetFilterForSpecificJob<MongoExtractJob>(jobIdentifier), existing);
                    if (!res.IsAcknowledged || res.ModifiedCount != 1)
                        throw new ApplicationException($"Received invalid ReplaceOneResult: {res}");
                    return;
                }

                var newJobInfo = new MongoExtractJob
                {
                    Header = jobHeader,
                    ExtractionJobIdentifier = requestInfoMessage.ExtractionJobIdentifier,
                    ProjectNumber = requestInfoMessage.ProjectNumber,
                    JobSubmittedAt = requestInfoMessage.JobSubmittedAt,
                    JobStatus = ExtractJobStatus.WaitingForCollectionInfo,
                    ExtractionDirectory = requestInfoMessage.ExtractionDirectory,
                    KeyTag = requestInfoMessage.KeyTag,
                    KeyCount = requestInfoMessage.KeyValueCount,
                    ExtractionModality = requestInfoMessage.ExtractionModality,
                    ExpectedFilesInfo = new List<MongoExpectedFilesForKey>(),
                    RejectedKeysInfo = new List<MongoRejectedKeyInfo>(),
                };

                _jobInfoCollection.InsertOne(newJobInfo);
            }
        }

        protected override void PersistMessageToStoreImpl(ExtractFileCollectionInfoMessage collectionInfoMessage, IMessageHeader header)
        {
            Guid jobId = collectionInfoMessage.ExtractionJobIdentifier;

            var expectedFilesForKey = new MongoExpectedFilesForKey
            {
                Header = ExtractFileCollectionHeader.FromMessageHeader(header, _dateTimeProvider),
                Key = collectionInfoMessage.KeyValue,
                AnonymisedFiles = collectionInfoMessage.ExtractFileMessagesDispatched
                    .Select(x => new ExpectedAnonymisedFileInfo
                    {
                        ExtractFileMessageGuid = x.Key.MessageGuid,
                        AnonymisedFilePath = x.Value
                    })
                    .ToList()
            };

            var rejectedKeyInfo = new MongoRejectedKeyInfo
            {
                Header = ExtractFileCollectionHeader.FromMessageHeader(header, _dateTimeProvider),
                Key = collectionInfoMessage.KeyValue,
                RejectionInfo = collectionInfoMessage.RejectionReasons
            };

            lock (_oJobStoreLock)
            {
                if (InArchiveCollection(jobId, out MongoExtractJob _) || InQuarantineCollection(jobId, out _))
                    throw new ApplicationException("Received an ExtractFileCollectionInfoMessage for a job that exists in the archive or quarantine");

                // Most likely already have an entry for this
                if (InJobCollection(jobId, out MongoExtractJob existing))
                {
                    existing.ExpectedFilesInfo.Add(expectedFilesForKey);
                    existing.RejectedKeysInfo.Add(rejectedKeyInfo);
                    existing.ReceivedCollectionInfoMessages += 1;

                    if (existing.ReceivedCollectionInfoMessages == existing.KeyCount)
                        existing.JobStatus = ExtractJobStatus.WaitingForStatuses;

                    ReplaceOneResult res = _jobInfoCollection.ReplaceOne(GetFilterForSpecificJob<MongoExtractJob>(jobId), existing);
                    if (!res.IsAcknowledged || res.ModifiedCount != 1)
                        throw new ApplicationException($"Received invalid ReplaceOneResult: {res}");

                    return;
                }

                // Else create a blank one with just the new MongoExpectedFilesForKey
                var newJobInfo = new MongoExtractJob
                {
                    Header = new MongoExtractJobHeader(),
                    ExtractionJobIdentifier = jobId,
                    JobStatus = ExtractJobStatus.WaitingForJobInfo,
                    ExpectedFilesInfo = new List<MongoExpectedFilesForKey> { expectedFilesForKey },
                    RejectedKeysInfo = new List<MongoRejectedKeyInfo> { rejectedKeyInfo },
                    KeyCount = -1,
                    ReceivedCollectionInfoMessages = 1,
                };

                if (newJobInfo.ReceivedCollectionInfoMessages == newJobInfo.KeyCount)
                    newJobInfo.JobStatus = ExtractJobStatus.WaitingForStatuses;

                _jobInfoCollection.InsertOne(newJobInfo);
            }
        }

        protected override void PersistMessageToStoreImpl(ExtractFileStatusMessage fileStatusMessage, IMessageHeader header)
        {
            var newStatus = new MongoExtractedFileStatusDocument
            {
                Header = MongoExtractedFileStatusHeaderDocument.FromMessageHeader(header, _dateTimeProvider),
                Status = fileStatusMessage.Status.ToString(),
                StatusMessage = fileStatusMessage.StatusMessage
            };

            string collectionName = StatusCollectionPrefix + fileStatusMessage.ExtractionJobIdentifier;
            IMongoCollection<MongoExtractedFileStatusDocument> statusCollection = _database.GetCollection<MongoExtractedFileStatusDocument>(collectionName);
            if (statusCollection.CountDocuments(FilterDefinition<MongoExtractedFileStatusDocument>.Empty) == 0)
            {
                // TODO(rkm 2020-02-25) If the collection doesn't exist then it could have been archived/killed already - check this if the count is 0
                Logger.Warn($"No other status messages recieved - see TODO");
            }
            statusCollection.InsertOne(newStatus);
        }

        protected override void PersistMessageToStoreImpl(IsIdentifiableMessage anonVerificationMessage, IMessageHeader header)
        {
            var newStatus = new MongoExtractedFileStatusDocument
            {
                Header = MongoExtractedFileStatusHeaderDocument.FromMessageHeader(header, _dateTimeProvider),
                AnonymisedFileName = anonVerificationMessage.AnonymisedFileName,
                Status = anonVerificationMessage.IsIdentifiable ? "Identifiable" : "Verified",
                // TODO(rkm 2020-02-27) Check what the actual format of this report is
                StatusMessage = anonVerificationMessage.Report,
            };

            string collectionName = StatusCollectionPrefix + anonVerificationMessage.ExtractionJobIdentifier;
            IMongoCollection<MongoExtractedFileStatusDocument> statusCollection = _database.GetCollection<MongoExtractedFileStatusDocument>(collectionName);
            if (statusCollection.CountDocuments(FilterDefinition<MongoExtractedFileStatusDocument>.Empty) == 0)
            {
                // TODO(rkm 2020-02-25) If the collection doesn't exist then it could have been archived/killed already - check this if the count is 0
                Logger.Warn($"No other status messages recieved - see TODO");
            }

            statusCollection.InsertOne(newStatus);
        }

        protected override List<ExtractJobInfo> GetLatestJobInfoImpl(Guid jobId = default)
        {
            // TODO(rkm 2020-02-27) Log what exactly we are waiting for
            // TODO(rkm 2020-02-27 Check why this is for WaitingForStatuses only
            FilterDefinition<MongoExtractJob> filter = Builders<MongoExtractJob>.Filter.Eq(x => x.JobStatus, ExtractJobStatus.WaitingForStatuses);

            // If we have been passed a specific GUID, search for that job only
            if (jobId != default(Guid))
                filter = filter & Builders<MongoExtractJob>.Filter.Eq(x => x.ExtractionJobIdentifier, jobId);

            lock (_oJobStoreLock)
            {
                long docsInCollection = _jobInfoCollection.CountDocuments(FilterDefinition<MongoExtractJob>.Empty);
                Logger.Debug(docsInCollection + " documents in the job collection");

                if (docsInCollection == 0)
                    return new List<ExtractJobInfo>();

                List<MongoExtractJob> jobs = GetExtractJobs(filter).Result;

                var toRet = new List<ExtractJobInfo>();

                foreach (MongoExtractJob job in jobs)
                {
                    try
                    {
                        IMongoCollection<MongoExtractedFileStatusDocument> statusCollection = _database.GetCollection<MongoExtractedFileStatusDocument>(StatusCollectionPrefix + job.ExtractionJobIdentifier);
                        List<MongoExtractedFileStatusDocument> statuses = statusCollection.Find(FilterDefinition<MongoExtractedFileStatusDocument>.Empty).ToList();
                        toRet.Add(MongoExtractJobInfoExtensions.FromMongoJobInfo(job, statuses));
                    }
                    catch (Exception e)
                    {
                        Logger.Error("Error with job data for " + job.ExtractionJobIdentifier + ". Sending to quarantine");
                        QuarantineJob(job.ExtractionJobIdentifier, e);
                    }
                }

                return toRet;
            }
        }

        protected override void CleanupJobDataImpl(Guid extractionJobIdentifier)
        {
            lock (_oJobStoreLock)
            {
                MongoExtractJob toArchive;
                if (!InJobCollection(extractionJobIdentifier, out toArchive))
                    throw new ApplicationException("Could not find job " + extractionJobIdentifier + " in the job store");

                // Convert to an archived job, and update the status
                var archiveJob = new ArchivedMongoExtractJob(toArchive)
                {
                    JobStatus = ExtractJobStatus.Archived,
                    ArchivedAt = _dateTimeProvider.UtcNow()
                };

                try
                {
                    _jobArchiveCollection.InsertOne(archiveJob);
                }
                catch (MongoDuplicateKeyException e)
                {
                    throw new ApplicationException("Extract job " + extractionJobIdentifier + "was already present in the archive", e);
                }

                DeleteResult a = _jobInfoCollection.DeleteOne(GetFilterForSpecificJob<MongoExtractJob>(extractionJobIdentifier));

                if (!(a.IsAcknowledged || a.DeletedCount != 1))
                    throw new Exception("Job data was archived but could not delete original from job store");
            }

            Logger.Debug("Job " + extractionJobIdentifier + " archived");
        }

        protected override void QuarantineJobImpl(Guid extractionJobIdentifier, Exception cause)
        {
            // TODO(rkm 2020-02-06) Make this transactional

            lock (_oJobStoreLock)
            {
                MongoExtractJob toQuarantine;
                if (!InJobCollection(extractionJobIdentifier, out toQuarantine))
                    throw new ApplicationException("Could not find job " + extractionJobIdentifier + " in the job store");

                var quarantineInfo = new QuarantinedMongoExtractJob(toQuarantine, cause);

                try
                {
                    _jobQuarantineCollection.InsertOne(quarantineInfo);
                }
                catch (MongoDuplicateKeyException e)
                {
                    throw new ApplicationException("Extract job " + extractionJobIdentifier + "was already present in the quarantine", e);
                }

                DeleteResult a = _jobInfoCollection.DeleteOne(GetFilterForSpecificJob<MongoExtractJob>(extractionJobIdentifier));

                if (!(a.IsAcknowledged || a.DeletedCount != 1))
                    throw new Exception("Job data was quarantined but could not delete original from job store");
            }

            Logger.Debug("Job " + extractionJobIdentifier + " quarantined");
        }

        #region Helper Methods

        private static FilterDefinition<T> GetFilterForSpecificJob<T>(Guid extractionJobIdentifier) where T : MongoExtractJob
        {
            return Builders<T>.Filter.Eq(x => x.ExtractionJobIdentifier, extractionJobIdentifier);
        }

        private bool InJobCollection(Guid extractionJobIdentifier, out MongoExtractJob mongoExtractJob)
        {
            mongoExtractJob = _jobInfoCollection.Find(GetFilterForSpecificJob<MongoExtractJob>(extractionJobIdentifier))
                .SingleOrDefault();

            return mongoExtractJob != null;
        }

        private bool InArchiveCollection(Guid extractionJobIdentifier, out MongoExtractJob mongoExtractJob)
        {
            mongoExtractJob = _jobArchiveCollection
                .Find(GetFilterForSpecificJob<ArchivedMongoExtractJob>(extractionJobIdentifier)).SingleOrDefault();

            return mongoExtractJob != null;
        }

        private bool InQuarantineCollection(Guid extractionJobIdentifier, out MongoExtractJob mongoExtractJob)
        {
            mongoExtractJob = _jobQuarantineCollection
                .Find(GetFilterForSpecificJob<QuarantinedMongoExtractJob>(extractionJobIdentifier)).SingleOrDefault();

            return mongoExtractJob != null;
        }

        private async Task<List<MongoExtractJob>> GetExtractJobs(FilterDefinition<MongoExtractJob> filter)
        {
            var toReturn = new List<MongoExtractJob>();

            using (IAsyncCursor<MongoExtractJob> cursor = await _jobInfoCollection.FindAsync(filter, _findOptions))
            {
                while (await cursor.MoveNextAsync())
                    toReturn.AddRange(cursor.Current);
            }

            return toReturn;
        }

        #endregion
    }
}