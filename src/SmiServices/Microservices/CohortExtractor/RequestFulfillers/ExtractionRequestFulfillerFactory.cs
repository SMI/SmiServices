using Rdmp.Core.Curation.Data;
using System;
using System.Text.RegularExpressions;

namespace SmiServices.Microservices.CohortExtractor.RequestFulfillers;

public static class ExtractionRequestFulfillerFactory
{
    public static IExtractionRequestFulfiller Create(ExtractionRequestFulfillerType extractionRequestFulfillerType, ICatalogue[] catalogues, Regex? modalityRoutingRegex)
    {
        return extractionRequestFulfillerType switch
        {
            ExtractionRequestFulfillerType.FromCataloguesExtractionRequestFulfiller => new FromCataloguesExtractionRequestFulfiller(catalogues, modalityRoutingRegex),
            _ => throw new NotImplementedException($"No case for '{extractionRequestFulfillerType}'"),
        };
    }
}
