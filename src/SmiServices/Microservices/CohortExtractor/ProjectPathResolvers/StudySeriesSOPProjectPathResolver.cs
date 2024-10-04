using SmiServices.Common.Messages.Extraction;
using SmiServices.Microservices.CohortExtractor.RequestFulfillers;
using System.IO.Abstractions;

namespace SmiServices.Microservices.CohortExtractor.ProjectPathResolvers;

/// <summary>
/// Generates output paths in the form:
/// StudyInstanceUID/SeriesInstanceUID/SOPInstanceUID-an.dcm
/// </summary>
public class StudySeriesSOPProjectPathResolver : IProjectPathResolver
{
    private readonly IFileSystem _fileSystem;

    public StudySeriesSOPProjectPathResolver(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    /// <inheritdoc/>    
    public string GetOutputPath(QueryToExecuteResult result, ExtractionRequestMessage request)
    {
        string extToUse = request.IsIdentifiableExtraction ? ProjectPathResolverConstants.IDENT_EXT : ProjectPathResolverConstants.ANON_EXT;

        return _fileSystem.Path.Combine(
            result.StudyTagValue,
            result.SeriesTagValue,
            $"{result.InstanceTagValue}{extToUse}"
        );
    }
}
