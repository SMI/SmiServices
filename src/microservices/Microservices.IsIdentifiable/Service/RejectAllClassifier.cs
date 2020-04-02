using System.Collections.Generic;
using System.IO;
using Microservices.IsIdentifiable.Failures;
using Microservices.IsIdentifiable.Reporting;

namespace Microservices.IsIdentifiable.Service
{
    public class RejectAllClassifier: Classifier
    {
        public RejectAllClassifier(DirectoryInfo dataDirectory) : base(dataDirectory)
        {
        }

        public override IEnumerable<Failure> Classify(FileInfo dcm)
        {
            yield return new Failure(new []{new FailurePart("Reject All classifier rejected all content",FailureClassification.Person)});
        }
    }
}