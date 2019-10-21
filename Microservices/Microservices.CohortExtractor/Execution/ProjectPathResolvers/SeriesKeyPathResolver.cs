
namespace Microservices.CohortExtractor.Execution.ProjectPathResolvers
{
    public class SeriesKeyPathResolver : DefaultProjectPathResolver
    {
        public override string GetSubdirectory(ExtractImageCollection collection)
        {
            return collection.SeriesInstanceUID;
        }
    }
}
