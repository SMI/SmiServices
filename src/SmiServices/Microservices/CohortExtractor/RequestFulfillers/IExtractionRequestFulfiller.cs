using SmiServices.Common.Messages.Extraction;
using SmiServices.Common.Options;
using System.Collections.Generic;

namespace SmiServices.Microservices.CohortExtractor.RequestFulfillers;

public interface IExtractionRequestFulfiller
{
    /// <summary>
    /// When implemented in a derived class will connect to data sources and return all dicom files on disk which
    /// correspond to the identifiers in the <paramref name="message"/>.
    /// </summary>
    /// <param name="message">The request you want answered (contains the list of UIDs to extract)</param>
    /// <returns></returns>
    IEnumerable<ExtractImageCollection> GetAllMatchingFiles(ExtractionRequestMessage message);

    /// <summary>
    /// Controls what records that are fetched back should be reported as non extractable (including the reason why)
    /// </summary>
    List<IRejector> Rejectors { get; set; }

    /// <summary>
    /// Collection of <see cref="IRejector"/> that are to be used only on specific Modality(s) either instead of or
    /// in addition to the basic <see cref="Rejectors"/>
    /// </summary>
    Dictionary<ModalitySpecificRejectorOptions, IRejector> ModalitySpecificRejectors { get; set; }
}
