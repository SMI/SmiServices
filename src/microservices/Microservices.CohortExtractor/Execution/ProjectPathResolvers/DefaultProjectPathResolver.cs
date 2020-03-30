
using System;
using System.IO;
using MathNet.Numerics;
using Microservices.CohortExtractor.Execution.RequestFulfillers;
using Smi.Common.Messages.Extraction;

namespace Microservices.CohortExtractor.Execution.ProjectPathResolvers
{
    public class DefaultProjectPathResolver : IProjectPathResolver
    {
        public virtual string GetAnonymousDicomFilename(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException("fileName");

            //Dicom is flexible about file extensions.  It can be .dcm .dicom or nothing at all.  Lets standardise our output at least to -an.dcm
                        
            if(fileName.EndsWith(".dicom"))
                return fileName.Replace(".dicom", "-an.dcm");

            if (fileName.EndsWith(".dcm"))
                return fileName.Replace(".dcm", "-an.dcm");

            return fileName+"-an.dcm";
        }
        
        public string GetOutputPath(QueryToExecuteResult result,ExtractionRequestMessage request)
        {
            string anonFilename = GetAnonymousDicomFilename(Path.GetFileName(result.FilePathValue));

            //if all we know is the path
            if (result.StudyTagValue == null && result.SeriesTagValue == null)
                return anonFilename;

            if (result.StudyTagValue != null && result.SeriesTagValue != null)
                return Path.Combine(result.StudyTagValue, result.SeriesTagValue, anonFilename);

            //output it under study or series UID only then filename
            return Path.Combine(result.StudyTagValue ?? result.SeriesTagValue, anonFilename);
        }
    }
}
