using System.Collections.Generic;
using System.IO;
using Microservices.IsIdentifiable.Failure;

namespace Microservices.IsIdentifiable.Service
{
    public class RejectAllClassifier: Classifier
    {
        public RejectAllClassifier(DirectoryInfo dataDirectory) : base(dataDirectory)
        {
        }

        public override IEnumerable<FailurePart> Classify(FileInfo dcm)
        {
            yield return new FailurePart("Reject All classifier rejected all content",FailureClassification.Person);
        }
    }
}