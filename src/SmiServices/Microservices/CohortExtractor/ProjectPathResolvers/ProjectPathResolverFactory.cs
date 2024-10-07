using System;
using System.IO.Abstractions;

namespace SmiServices.Microservices.CohortExtractor.ProjectPathResolvers;
internal class ProjectPathResolverFactory
{
    public static IProjectPathResolver Create(string projectPathResolverType, IFileSystem fileSystem)
    {
        return projectPathResolverType switch
        {
            nameof(StudySeriesSOPProjectPathResolver) => new StudySeriesSOPProjectPathResolver(fileSystem),
            nameof(NoSuffixProjectPathResolver) => new NoSuffixProjectPathResolver(fileSystem),
            nameof(StudySeriesOriginalFilenameProjectPathResolver) => new StudySeriesOriginalFilenameProjectPathResolver(fileSystem),
            _ => throw new NotImplementedException($"No case for IProjectPathResolver type '{projectPathResolverType}'"),
        };
    }
}
