
using Microservices.CohortExtractor.Audit;
using Smi.Common.Messages.Extraction;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Microservices.CohortExtractor.Execution.RequestFulfillers
{
    public interface IExtractionRequestFulfiller
    {
        /// <summary>
        /// When implemented in a derived class will connect to data sources and return all dicom files on disk which
        /// correspond to the identifiers in the <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The request you want answered (contains the list of UIDs to extract)</param>
        /// <param name="auditor">The class we should inform of progress</param>
        /// <returns></returns>
        IEnumerable<ExtractImageCollection> GetAllMatchingFiles(ExtractionRequestMessage message, IAuditExtractions auditor);

        /// <summary>
        /// Controls what records that are fetched back should be reported as non extractable (including the reason why)
        /// </summary>
        IRejector Rejector { get; set; }

        /// <summary>
        /// Controls how modalities are matched to Catalogues.  Must contain a single capture group which
        /// returns a modality code (e.g. CT) when applies to a Catalogue name.  E.g. ^([A-Z]+)_.*$ would result
        /// in Modalities being routed based on the start of the table name e.g. CT => CT_MyTable and MR=> MR_MyTable
        /// </summary>
        Regex ModalityRoutingRegex { get; set; }
    }
}