using IsIdentifiable.Failures;
using IsIdentifiable.Options;
using System.Collections.Generic;
using System.IO.Abstractions;

namespace SmiServices.Microservices.IsIdentifiable
{
    public class RejectAllClassifier : Classifier
    {
        public RejectAllClassifier(IDirectoryInfo dataDirectory, IsIdentifiableDicomFileOptions _) : base(dataDirectory)
        {
        }

        public override IEnumerable<Failure> Classify(IFileInfo dcm)
        {
            yield return new Failure([new FailurePart("Reject All classifier rejected all content", FailureClassification.Person)]);
        }
    }
}
