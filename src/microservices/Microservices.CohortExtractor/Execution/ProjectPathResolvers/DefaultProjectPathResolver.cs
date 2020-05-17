
using Microservices.CohortExtractor.Execution.RequestFulfillers;
using Smi.Common.Messages.Extraction;
using System;
using System.IO;

namespace Microservices.CohortExtractor.Execution.ProjectPathResolvers
{
    public class DefaultProjectPathResolver : IProjectPathResolver
    {
        public const string AnonExt = "-an.dcm";

        private static readonly string[] _replaceableExtensions = { ".dcm", ".dicom" };


        public string GetOutputPath(QueryToExecuteResult result, ExtractionRequestMessage request)
        {
            string requestOutputDir = Path.GetFileNameWithoutExtension(request.ExtractionName);
            string anonFilename = GetAnonymousDicomFilename(Path.GetFileName(result.FilePathValue));
            return Path.Combine(
                requestOutputDir,
                "image-requests",
                result.StudyTagValue ?? "unknown", 
                result.SeriesTagValue ?? "unknown", 
                anonFilename);
        }

        /// <summary>
        /// The extension of the input DICOM file can be anything (or nothing), but here we try to standardise the output (anonymised) file name to be -an.dcm
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private static string GetAnonymousDicomFilename(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException(nameof(fileName));

            foreach (string ext in _replaceableExtensions)
                if (fileName.EndsWith(ext))
                    return fileName.Replace(ext, AnonExt);

            return fileName + AnonExt;
        }
    }
}
