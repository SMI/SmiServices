using SmiServices.Common.Messages.Extraction;
using SmiServices.Microservices.CohortExtractor.RequestFulfillers;

namespace SmiServices.Microservices.CohortExtractor.ProjectPathResolvers;

public interface IProjectPathResolver
{
    /// <summary>
    /// Get the output path, build from the original file path, plus any separating
    /// directories (such as by SeriesInstanceUID)
    /// </summary>
    /// <param name="result">The file path (and UIDs) of the original dcm file in the identifiable repository.  Some portions
    /// of the <paramref name="result"/> may be null e.g. StudyInstanceUID if the corresponding column does not appear in the
    /// extraction table which this result was fetched from</param>
    /// <param name="request">Contains information about the original request e.g. project number</param>
    /// <returns></returns>
    string GetOutputPath(QueryToExecuteResult result, ExtractionRequestMessage request);
}
