using System.Collections.Generic;
using System.Linq;
using Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB.ObjectModel;


namespace Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB
{
    public static class MongoExtractJobInfoExtensions
    {
        public static ExtractJobInfo FromMongoJobInfo(MongoExtractJob mongoExtractJob, List<MongoExtractedFileStatusDocument> jobStatuses) =>
            new ExtractJobInfo(
                mongoExtractJob.ExtractionJobIdentifier,
                mongoExtractJob.ProjectNumber,
                mongoExtractJob.JobSubmittedAt,
                mongoExtractJob.JobStatus,
                mongoExtractJob.ExtractionDirectory,
                mongoExtractJob.KeyCount,
                mongoExtractJob.KeyTag,
                FromMongoExtractJob(mongoExtractJob),
                FromMongoStatusList(jobStatuses),
                mongoExtractJob.ExtractionModality);

        private static List<ExtractFileCollectionInfo> FromMongoExtractJob(MongoExtractJob mongoExtractJob)
        {
            return mongoExtractJob
                .ExpectedFilesInfo
                .Select(fileColl => new ExtractFileCollectionInfo(
                    fileColl.Key,
                    fileColl.AnonymisedFiles
                        .Select(fileInfo => fileInfo.AnonymisedFilePath)
                        .ToList()))
                .ToList();
        }

        private static List<ExtractFileStatusInfo> FromMongoStatusList(IEnumerable<MongoExtractedFileStatusDocument> jobStatuses) =>
            jobStatuses
                .Select(x => new ExtractFileStatusInfo(
                    x.Status,
                    x.AnonymisedFileName,
                    x.StatusMessage))
                .ToList();
    }
}
