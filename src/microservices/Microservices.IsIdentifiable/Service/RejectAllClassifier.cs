using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using IsIdentifiable.Failures;
using IsIdentifiable.Reporting;
using Smi.Common.Options;

namespace Microservices.IsIdentifiable.Service
{
    public class RejectAllClassifier: Classifier
    {
        public RejectAllClassifier(DirectoryInfo dataDirectory, IsIdentifiableServiceOptions _) : base(dataDirectory)
        {
        }

        public override IEnumerable<Failure> Classify(IFileInfo dcm)
        {
            yield return new Failure(new []{new FailurePart("Reject All classifier rejected all content",FailureClassification.Person)});
        }
    }
}