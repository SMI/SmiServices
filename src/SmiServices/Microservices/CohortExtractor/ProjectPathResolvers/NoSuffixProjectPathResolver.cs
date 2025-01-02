using SmiServices.Common.Messages.Extraction;
using SmiServices.Microservices.CohortExtractor.RequestFulfillers;
using System.IO.Abstractions;

namespace SmiServices.Microservices.CohortExtractor.ProjectPathResolvers;

/// <summary>
/// Acts like <see cref="StudySeriesOriginalFilenameProjectPathResolver"/> but with no "-an" component to indicate files have been anonymised.  In most cases this results in an identical filename to the source file but can include the addition of a .dcm extension where it is missing
/// </summary>
public class NoSuffixProjectPathResolver : StudySeriesOriginalFilenameProjectPathResolver
{
    public NoSuffixProjectPathResolver(IFileSystem fileSystem) : base(fileSystem) { }

    public override string GetOutputPath(QueryToExecuteResult result, ExtractionRequestMessage message)
    {
        return base.GetOutputPath(result, message).Replace("-an", "");
    }
}
