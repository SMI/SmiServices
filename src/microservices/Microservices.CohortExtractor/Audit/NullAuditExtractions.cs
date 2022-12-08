using Microservices.CohortExtractor.Execution;
using Smi.Common.Messages.Extraction;
using Rdmp.Core.Curation.Data;

namespace Microservices.CohortExtractor.Audit
{
    /// <summary>
    /// Implementation of <see cref="IAuditExtractions"/> that does nothing (no auditing).
    /// </summary>
    public class NullAuditExtractions : IAuditExtractions
    {
        /// <inheritdoc/>
        public void AuditExtractionRequest(ExtractionRequestMessage message)
        {
            return;
        }
        /// <inheritdoc/>
        public void AuditExtractFiles(ExtractionRequestMessage request, ExtractImageCollection answers)
        {
            return;
        }
        /// <inheritdoc/>
        public void AuditCatalogueUse(ExtractionRequestMessage message, ICatalogue catalogue)
        {
            return;
        }
    }
}
