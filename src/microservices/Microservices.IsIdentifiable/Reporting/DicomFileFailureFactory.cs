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
            return new Failure(parts)
            {
                Resource = file.FullName,
                ResourcePrimaryKey = dcm.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID),
                ProblemValue = problemValue,
                ProblemField = problemField
            };
        }
    }
}