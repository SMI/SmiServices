using System.IO.Abstractions;

namespace SmiServices.Microservices.CohortExtractor.ProjectPathResolvers
{
    /// <summary>
    /// Acts like <see cref="DefaultProjectPathResolver"/> but with no "-an" component to indicate files have been anonymised.  In most cases this results in an identical filename to the source file but can include the addition of a .dcm extension where it is missing
    /// </summary>
    public class NoSuffixProjectPathResolver : DefaultProjectPathResolver
    {
        public NoSuffixProjectPathResolver(IFileSystem fileSystem)
            : base(fileSystem)
        {
            AnonExt = ".dcm";
        }
    }
}
