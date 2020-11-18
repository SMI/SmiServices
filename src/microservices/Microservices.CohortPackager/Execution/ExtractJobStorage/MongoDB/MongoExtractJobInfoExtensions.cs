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
                mongoExtractJobDoc.JobStatus,
                mongoExtractJobDoc.IsIdentifiableExtraction,
                mongoExtractJobDoc.IsNoFilterExtraction
                );

        public static CompletedExtractJobInfo ToExtractJobInfo(this MongoCompletedExtractJobDoc mongoCompletedExtractJobDoc)
            => new CompletedExtractJobInfo(
                mongoCompletedExtractJobDoc.ExtractionJobIdentifier,
                mongoCompletedExtractJobDoc.JobSubmittedAt,
                mongoCompletedExtractJobDoc.CompletedAt,
                mongoCompletedExtractJobDoc.ProjectNumber,
                mongoCompletedExtractJobDoc.ExtractionDirectory,
                mongoCompletedExtractJobDoc.KeyTag,
                mongoCompletedExtractJobDoc.KeyCount,
                mongoCompletedExtractJobDoc.ExtractionModality,
                mongoCompletedExtractJobDoc.IsIdentifiableExtraction,
                mongoCompletedExtractJobDoc.IsNoFilterExtraction
            );
    }
}
