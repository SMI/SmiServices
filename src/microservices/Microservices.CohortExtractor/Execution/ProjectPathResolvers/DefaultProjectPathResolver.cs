
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

        /// <summary>
        /// Returns the output path for the anonymised file, relative to the ExtractionDirectory
        /// </summary>
        /// <param name="result"></param>
        /// <param name="_"></param>
        /// <returns></returns>
        public string GetOutputPath(QueryToExecuteResult result, ExtractionRequestMessage _)
        {
            // The extension of the input DICOM file can be anything (or nothing), but here we try to standardise the output (anonymised) file name to be -an.dcm
            string fileName = Path.GetFileName(result.FilePathValue);
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException(nameof(fileName));

            var replaced = false;
            foreach (string ext in _replaceableExtensions)
                if (fileName.EndsWith(ext))
                {
                    fileName = fileName.Replace(ext, AnonExt);
                    replaced = true;
                    break;
                }

            if (!replaced)
                fileName += AnonExt;

            return Path.Combine(
                result.StudyTagValue ?? "unknown",
                result.SeriesTagValue ?? "unknown",
                fileName);
        }
    }
}
