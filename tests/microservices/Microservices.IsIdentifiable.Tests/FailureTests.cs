using Microservices.IsIdentifiable.Failures;
using NUnit.Framework;
using Microservices.IsIdentifiable.Reporting;

namespace Microservices.IsIdentifiable.Tests
{
    public class FailureTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void OverlappingMatches_SinglePart(bool includeExact)
        {
            Failure f = new Failure(new[]
            {
                new FailurePart("F",FailureClassification.Person,0),
            })
                {ProblemValue = "Frequent Problems"};

            Assert.IsFalse(f.HasOverlappingParts(includeExact));
        }
        [TestCase(true)]
        [TestCase(false)]
        public void OverlappingMatches_ExactOverlap(bool includeExact)
        {
            Failure f = new Failure(new[]
                {
                    new FailurePart("Freq",FailureClassification.Person,0),
                    new FailurePart("Freq",FailureClassification.Organization,0),
                })
                {ProblemValue = "Frequent Problems"};

            Assert.AreEqual(includeExact,f.HasOverlappingParts(includeExact));
        }
        [TestCase(true)]
        [TestCase(false)]
        public void OverlappingMatches_OffsetOverlaps(bool includeExact)
        {
            Failure f = new Failure(new[]
                {
                    new FailurePart("Freq",FailureClassification.Person,0),
                    new FailurePart("q",FailureClassification.Organization,3),
                })
                {ProblemValue = "Frequent Problems"};

            Assert.IsTrue(f.HasOverlappingParts(includeExact));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void OverlappingMatches_NoOverlaps(bool includeExact)
        {
            Failure f = new Failure(new[]
                {
                    new FailurePart("Fre",FailureClassification.Person,0),
                    new FailurePart("quent",FailureClassification.Organization,3),
                })
                {ProblemValue = "Frequent Problems"};

            Assert.IsFalse(f.HasOverlappingParts(includeExact));
        }
    }
}