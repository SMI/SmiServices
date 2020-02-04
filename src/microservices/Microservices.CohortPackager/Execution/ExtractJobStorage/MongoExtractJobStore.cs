
using Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDocuments;
using MongoDB.Bson;
using MongoDB.Driver;
using NLog;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.MongoDB;
using Smi.Common.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Microservices.CohortPackager.Execution.ExtractJobStorage
{
    public class MongoExtractJobStore : IExtractJobStore
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        private readonly IMongoDatabase _database;
        private readonly object _oDbLock = new object();

        private const string ExtractJobCollectionName = "extractJobStore";
        private const string QuarantineCollectionName = "extractJobQuarantine";
        private const string ArchiveCollectionName = "extractJobArchive";
        private const string StatusCollectionPrefix = "statuses_";

        private readonly IMongoCollection<MongoExtractJob> _jobInfoCollection;
        private readonly IMongoCollection<ArchivedMongoExtractJob> _jobArchiveCollection;
        private readonly IMongoCollection<QuarantinedMongoExtractJob> _jobQuarantineCollection;

        private readonly FindOptions<MongoExtractJob> _findOptions = new FindOptions<MongoExtractJob>
        {
            BatchSize = 1,
            NoCursorTimeout = false
        };


        public MongoExtractJobStore(MongoDbOptions options)
        {
            MongoClient client = MongoClientHelpers.GetMongoClient(options, "CohortPackager");
            _database = client.GetDatabase(options.DatabaseName);

            try
            {
                Ping();
            }
            catch (ApplicationException e)
            {
                throw new ArgumentException(
                    "Could not connect to the MongoDB server/database on startup at: " + options.HostName + ":" +
                    options.Port, e);
            }

            _jobInfoCollection = _database.GetCollection<MongoExtractJob>(ExtractJobCollectionName);
            _jobArchiveCollection = _database.GetCollection<ArchivedMongoExtractJob>(ArchiveCollectionName);
            _jobQuarantineCollection = _database.GetCollection<QuarantinedMongoExtractJob>(QuarantineCollectionName);

            long count = _jobInfoCollection.CountDocuments(FilterDefinition<MongoExtractJob>.Empty);

            _logger.Info(
                count > 0
                ? "Connected to job store with " + count + " existing jobs"
                : "Empty job store created successfully");
        }

        #region Helper Methods

        private void Ping(int timeout = 1000)
        {
            bool isLive = _database.RunCommandAsync((Command<BsonDocument>)"{ping:1}").Wait(timeout);

            if (!isLive)
                throw new ApplicationException("Could not ping MongoDB");
        }

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

        private static ExtractJobInfo BuildJobInfo(MongoExtractJob mongoExtractJob, List<MongoExtractedFileStatus> jobStatuses)
        {
            return new ExtractJobInfo(
                mongoExtractJob.ExtractionJobIdentifier,
                mongoExtractJob.ProjectNumber,
                mongoExtractJob.JobSubmittedAt,
                mongoExtractJob.JobStatus,
                mongoExtractJob.ExtractionDirectory,
                mongoExtractJob.KeyValueCount,
                mongoExtractJob.KeyTag,
                BuildFileCollectionInfoList(mongoExtractJob),
                BuildFileStatusInfoList(jobStatuses));
        }

        private static List<ExtractFileCollectionInfo> BuildFileCollectionInfoList(MongoExtractJob mongoExtractJob)
        {
            return mongoExtractJob
                .FileCollectionInfo
                .Select(fileColl => new ExtractFileCollectionInfo(
                    fileColl.KeyValue,
                    fileColl.AnonymisedFiles
                        .Select(fileInfo => fileInfo.AnonymisedFilePath)
                        .ToList()))
                .ToList();
        }

        private static List<ExtractFileStatusInfo> BuildFileStatusInfoList(IEnumerable<MongoExtractedFileStatus> jobStatuses)
        {
            return jobStatuses.Select(x => new ExtractFileStatusInfo(
                    x.Status,
                    x.AnonymisedFileName,
                    x.StatusMessage))
                .ToList();
        }

        #endregion

        public void PersistMessageToStore(ExtractionRequestInfoMessage requestInfoMessage, IMessageHeader header)
        {
            Guid jobIdentifier = requestInfoMessage.ExtractionJobIdentifier;

            var jobHeader = new ExtractJobHeader
            {
                ExtractRequestInfoMessageGuid = header.MessageGuid,
                ProducerIdentifier = header.ProducerExecutableName + "(" + header.ProducerProcessID + ")",
                ReceivedAt = DateTime.Now
            };

            lock (_oDbLock)
            {
                MongoExtractJob _;
                if (InArchiveCollection(jobIdentifier, out _) || InQuarantineCollection(jobIdentifier, out _))
                    throw new ApplicationException(
                        "Received an ExtractionRequestInfoMessage for a job that exists in the archive or quarantine");

                MongoExtractJob existing;
                if (InJobCollection(jobIdentifier, out existing))
                {
                    if (existing.JobStatus != ExtractJobStatus.WaitingForJobInfo)
                        throw new ApplicationException(
                            "Received ExtractionRequestInfoMessage for a job which has already started processing");

                    existing.Header = jobHeader;
                    existing.ProjectNumber = requestInfoMessage.ProjectNumber;
                    existing.ExtractionDirectory = requestInfoMessage.ExtractionDirectory;
                    existing.KeyValueCount = requestInfoMessage.KeyValueCount;

                    if (existing.KeyValueCount == existing.FileCollectionInfo.Count)
                        existing.JobStatus = ExtractJobStatus.WaitingForFiles;

                    _jobInfoCollection.ReplaceOne(GetFilterForSpecificJob<MongoExtractJob>(jobIdentifier), existing);

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
                    KeyValueCount = requestInfoMessage.KeyValueCount,
                    FileCollectionInfo = new List<MongoExtractFileCollection>()
                };

                _jobInfoCollection.InsertOne(newJobInfo);
            }
        }

        public void PersistMessageToStore(ExtractFileCollectionInfoMessage collectionInfoMessage, IMessageHeader header)
        {
            Guid jobIdentifier = collectionInfoMessage.ExtractionJobIdentifier;
            var expectedFiles = new List<ExpectedAnonymisedFileInfo>();

            // Extract the list of expected anonymised files from the message
            collectionInfoMessage.ExtractFileMessagesDispatched.ToList().ForEach(
                x => expectedFiles.Add(
                    new ExpectedAnonymisedFileInfo
                    {
                        ExtractFileMessageGuid = x.Key.MessageGuid,
                        AnonymisedFilePath = x.Value
                    }));

            var newFileCollectionInfo = new MongoExtractFileCollection
            {
                Header = new ExtractFileCollectionHeader
                {
                    ExtractFileCollectionInfoMessageGuid = header.MessageGuid,
                    ProducerIdentifier = header.ProducerExecutableName + "(" + header.ProducerProcessID + ")",
                    ReceivedAt = DateTime.Now
                },

                KeyValue = collectionInfoMessage.KeyValue,
                AnonymisedFiles = expectedFiles
            };

            lock (_oDbLock)
            {
                MongoExtractJob _;
                if (InArchiveCollection(jobIdentifier, out _) || InQuarantineCollection(jobIdentifier, out _))
                    throw new ApplicationException(
                        "Received an ExtractFileCollectionInfoMessage for a job that exists in the archive or quarantine");

                // Most likely already have an entry for this

                MongoExtractJob existing;
                if (InJobCollection(jobIdentifier, out existing))
                {
                    existing.FileCollectionInfo.Add(newFileCollectionInfo);

                    if (existing.FileCollectionInfo.Count == existing.KeyValueCount)
                        existing.JobStatus = ExtractJobStatus.WaitingForFiles;

                    _jobInfoCollection.ReplaceOne(GetFilterForSpecificJob<MongoExtractJob>(jobIdentifier), existing);

                    return;
                }

                // Else create a blank one with just the new MongoExtractFileCollection

                var newJobInfo = new MongoExtractJob
                {
                    ExtractionJobIdentifier = jobIdentifier,
                    JobStatus = ExtractJobStatus.WaitingForJobInfo,
                    KeyTag = collectionInfoMessage.KeyValue,
                    FileCollectionInfo = new List<MongoExtractFileCollection> { newFileCollectionInfo }
                };

                _jobInfoCollection.InsertOne(newJobInfo);
            }
        }

        public void PersistMessageToStore(ExtractFileStatusMessage fileStatusMessage, IMessageHeader header)
        {
            string collectionName = StatusCollectionPrefix + fileStatusMessage.ExtractionJobIdentifier;

            var newStatus = new MongoExtractedFileStatus
            {
                Header = new ExtractFileStatusMessageHeader
                {
                    FileStatusMessageGuid = header.MessageGuid,
                    ProducerIdentifier = header.ProducerExecutableName + "(" + header.ProducerProcessID + ")",
                    ReceivedAt = DateTime.Now
                },

                Status = fileStatusMessage.Status.ToString(),
                AnonymisedFileName = fileStatusMessage.AnonymisedFileName,
                StatusMessage = fileStatusMessage.StatusMessage
            };

            lock (_oDbLock)
            {
                IMongoCollection<MongoExtractedFileStatus> statusCollection =
                    _database.GetCollection<MongoExtractedFileStatus>(collectionName);
                statusCollection.InsertOne(newStatus);
            }
        }

        public void PersistMessageToStore(IsIdentifiableMessage anonVerificationMessage, IMessageHeader header)
        {
            throw new NotImplementedException();
        }

        public List<ExtractJobInfo> GetLatestJobInfo(Guid extractionJobIdentifier = default(Guid))
        {
            _logger.Debug("Getting job info for " + (extractionJobIdentifier != Guid.Empty ? extractionJobIdentifier.ToString() : "all active jobs"));

            FilterDefinition<MongoExtractJob> filter = Builders<MongoExtractJob>.Filter.Eq(x => x.JobStatus, ExtractJobStatus.WaitingForFiles);

            // If we have been passed a specific GUID, search for that job only
            if (extractionJobIdentifier != default(Guid))
                filter = filter & Builders<MongoExtractJob>.Filter.Eq(x => x.ExtractionJobIdentifier, extractionJobIdentifier);

            lock (_oDbLock)
            {
                long docsInCollection = _jobInfoCollection.CountDocuments(FilterDefinition<MongoExtractJob>.Empty);
                _logger.Debug(docsInCollection + " documents in the job collection");

                if (docsInCollection == 0)
                    return new List<ExtractJobInfo>();

                List<MongoExtractJob> jobs = GetExtractJobs(filter).Result;

                var toRet = new List<ExtractJobInfo>();

                foreach (MongoExtractJob job in jobs)
                {
                    try
                    {
                        IMongoCollection<MongoExtractedFileStatus> statusCollection = _database.GetCollection<MongoExtractedFileStatus>(StatusCollectionPrefix + job.ExtractionJobIdentifier);
                        List<MongoExtractedFileStatus> statuses = statusCollection.Find(FilterDefinition<MongoExtractedFileStatus>.Empty).ToList();

                        toRet.Add(BuildJobInfo(job, statuses));
                    }
                    catch (Exception e)
                    {
                        _logger.Error("Error with job data for " + job.ExtractionJobIdentifier + ". Sending to quarantine");
                        QuarantineJob(job.ExtractionJobIdentifier, e);
                    }
                }

                return toRet;
            }
        }

        public void CleanupJobData(Guid extractionJobIdentifier)
        {
            _logger.Debug("Cleaning up job data for " + extractionJobIdentifier);

            lock (_oDbLock)
            {
                MongoExtractJob toArchive;
                if (!InJobCollection(extractionJobIdentifier, out toArchive))
                    throw new ApplicationException("Could not find job " + extractionJobIdentifier + " in the job store");

                // Convert to an archived job, and update the status
                var archiveJob = new ArchivedMongoExtractJob(toArchive, DateTime.UtcNow)
                {
                    JobStatus = ExtractJobStatus.Archived
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

            _logger.Debug("Job " + extractionJobIdentifier + " archived");
        }

        public void QuarantineJob(Guid extractionJobIdentifier, Exception cause)
        {
            _logger.Debug("Quarantining job data for " + extractionJobIdentifier);

            lock (_oDbLock)
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

            _logger.Debug("Job " + extractionJobIdentifier + " quarantined");
        }
    }
}