
using System;
using System.IO;

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

        public virtual string GetSubdirectory(ExtractImageCollection collection)
        {
            return "";
        }

        public string GetOutputPath(string filePath, ExtractImageCollection collection)
        {
            return Path.Combine(GetSubdirectory(collection),
                GetAnonymousDicomFilename(Path.GetFileName(filePath)));
        }
    }
}
