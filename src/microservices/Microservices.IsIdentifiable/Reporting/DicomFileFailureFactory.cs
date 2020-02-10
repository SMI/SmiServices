using System.Collections.Generic;
using System.IO;
using Dicom;
using Microservices.IsIdentifiable.Failures;

namespace Microservices.IsIdentifiable.Reporting
{
    class DicomFileFailureFactory
    {
        public Failure Create(FileInfo file, DicomFile dcm, string problemValue, string problemField, IEnumerable<FailurePart> parts)
        {
            string resourcePrimaryKey;
            try
            {
                // Some DICOM files do not have SOPInstanceUID
                resourcePrimaryKey = dcm.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID);
            }
            catch (DicomDataException e)
            {
                resourcePrimaryKey = "UnknownPrimaryKey";
            }
            return new Failure(parts)
            {
                Resource = file.FullName,
                ResourcePrimaryKey = resourcePrimaryKey,
                ProblemValue = problemValue,
                ProblemField = problemField
            };
        }
    }
}