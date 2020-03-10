using System;
using System.Linq;
using Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB.ObjectModel;

namespace Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB
{
    public static class MongoExtractJobInfoExtensions
    {
        public static ExtractJobInfo ToExtractJobInfo(this MongoExtractJobDoc mongoExtractJobDoc)
            => new ExtractJobInfo(
                mongoExtractJobDoc.ExtractionJobIdentifier,
                mongoExtractJobDoc.JobSubmittedAt,
                mongoExtractJobDoc.ProjectNumber,
                mongoExtractJobDoc.ExtractionDirectory,
                mongoExtractJobDoc.KeyTag,
                mongoExtractJobDoc.KeyCount,
                mongoExtractJobDoc.ExtractionModality,
                mongoExtractJobDoc.JobStatus);

        public static ExtractFileCollectionInfo ToExtractFileCollectionInfo(this MongoExpectedFilesDoc doc)
        {
            if (doc.RejectedKeys?.RejectionInfo == null)
                throw new ArgumentException(nameof(doc));

            return new ExtractFileCollectionInfo(
                doc.Key,
                doc.ExpectedFiles.Select(x => x.AnonymisedFilePath).ToList(),
                doc.RejectedKeys?.RejectionInfo
            );
        }

        public static ExtractFileStatusInfo ToExtractFileStatusInfo(this MongoFileStatusDoc doc)
            => new ExtractFileStatusInfo(doc.AnonymisedFileName, doc.IsIdentifiable, doc.StatusMessage);
    }
}
