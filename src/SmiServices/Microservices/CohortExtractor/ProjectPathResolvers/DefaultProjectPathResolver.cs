using SmiServices.Common.Messages.Extraction;
using SmiServices.Microservices.CohortExtractor.RequestFulfillers;
using System;
using System.IO;

namespace SmiServices.Microservices.CohortExtractor.ProjectPathResolvers
{
    public class DefaultProjectPathResolver : IProjectPathResolver
    {
        public string AnonExt { get; protected set; } = "-an.dcm";
        public string IdentExt { get; protected set; } = ".dcm";

        private static readonly string[] _replaceableExtensions = [".dcm", ".dicom"];

        /// <summary>
        /// Returns the output path for the anonymised file, relative to the ExtractionDirectory
        /// </summary>
        /// <param name="result"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public string GetOutputPath(QueryToExecuteResult result, ExtractionRequestMessage message)
        {
            string extToUse = message.IsIdentifiableExtraction ? IdentExt : AnonExt;

            // The extension of the input DICOM file can be anything (or nothing), but here we try to standardise the output file name to have the required extension
            string fileName = Path.GetFileName(result.FilePathValue);
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException(nameof(result));

            var replaced = false;
            foreach (string ext in _replaceableExtensions)
                if (fileName.EndsWith(ext))
                {
                    fileName = fileName.Replace(ext, extToUse);
                    replaced = true;
                    break;
                }

            if (!replaced)
                fileName += extToUse;

            // Remove any leading periods from the UIDs
            string? studyUID = result.StudyTagValue?.TrimStart('.');
            string? seriesUID = result.SeriesTagValue?.TrimStart('.');

            return Path.Combine(
                studyUID ?? "unknown",
                seriesUID ?? "unknown",
                fileName);
        }
    }
}
