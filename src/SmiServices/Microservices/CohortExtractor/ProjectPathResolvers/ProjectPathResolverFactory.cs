using System;
using System.IO.Abstractions;

namespace SmiServices.Microservices.CohortExtractor.ProjectPathResolvers;
internal class ProjectPathResolverFactory
{
    public static IProjectPathResolver Create(string projectPathResolverType, IFileSystem fileSystem)
    {
        return projectPathResolverType switch
        {
            "StudySeriesSOPProjectPathResolver" => new StudySeriesSOPProjectPathResolver(fileSystem),
            "NoSuffixProjectPathResolver" => new NoSuffixProjectPathResolver(fileSystem),
            "StudySeriesOriginalFilenameProjectPathResolver" => new StudySeriesOriginalFilenameProjectPathResolver(fileSystem),
            _ => throw new NotImplementedException($"No case for IProjectPathResolver type '{projectPathResolverType}'"),
        };
    }
}
