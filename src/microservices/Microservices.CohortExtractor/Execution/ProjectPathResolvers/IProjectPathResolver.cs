
namespace Microservices.CohortExtractor.Execution.ProjectPathResolvers
{
    internal interface IProjectPathResolver
    {
        /// <summary>
        /// Get the anonymous filename from the original filename
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        string GetAnonymousDicomFilename(string fileName);

        /// <summary>
        /// Get the subdirectory of the extraction directory to extract the file to
        /// </summary>
        /// <param name="request"></param>
        /// <param name="separators">Array of strings to use when building the full output path</param>
        /// <returns></returns>
        string GetSubdirectory(ExtractImageCollection collection);

        /// <summary>
        /// Get the output path, build from the original file path, plus any separating
        /// directories (such as by SeriesInstanceUID)
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        string GetOutputPath(string filePath, ExtractImageCollection collection);
    }
}
