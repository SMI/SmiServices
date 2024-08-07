using Rdmp.Core.Curation.Data;
using SmiServices.Common.Messages.Extraction;

namespace SmiServices.Microservices.CohortExtractor.Audit
{
    /// <summary>
    /// Audits the generation of <see cref="ExtractionRequestMessage"/> generated by the cohort extraction process
    /// </summary>
    public interface IAuditExtractions
    {
        /// <summary>
        /// Audit the fact that a unique ExtractionRequestMessage was received (1 audit record per command line execution uniquely identified by <see cref="ExtractMessage.ExtractionJobIdentifier"/>).
        /// </summary>
        /// <param name="message"></param>
        void AuditExtractionRequest(ExtractionRequestMessage message);

        /// <summary>
        /// Audit (incrementally) the matched records found when satisfying the ExtractionRequestMessage
        /// </summary>
        void AuditExtractFiles(ExtractionRequestMessage request, ExtractImageCollection answers);

        /// <summary>
        /// Audit the fact that records have been extracted from the following dataset to satisfy the ExtractionRequestMessage
        /// </summary>
        /// <param name="message"></param>
        /// <param name="catalogue"></param>
        void AuditCatalogueUse(ExtractionRequestMessage message, ICatalogue catalogue);
    }
}
