using IsIdentifiableReviewer.Out;
using Microservices.IsIdentifiable.Failures;
using Microservices.IsIdentifiable.Reporting;
using NUnit.Framework;

namespace Microservices.IsIdentifiable.Tests.ReviewerTests
{
    public class MatchProblemValuesPatternFactoryTests
    {
        [Test]
        public void OverlappingMatches_SinglePart()
        {
            Failure f = new Failure(new[]
                {
                    new FailurePart("F",FailureClassification.Person,0),
                })
                {ProblemValue = "Frequent Problems"};

            var factory = new MatchProblemValuesPatternFactory();
            Assert.AreEqual("^(F)",factory.GetPattern(null,f));
        }

        [Test]
        public void OverlappingMatches_ExactOverlap()
        {
            Failure f = new Failure(new[]
                {
                    new FailurePart("Freq",FailureClassification.Person,0),
                    new FailurePart("Freq",FailureClassification.Organization,0),
                })
                {ProblemValue = "Frequent Problems"};

            var factory = new MatchProblemValuesPatternFactory();
            Assert.AreEqual("^(Freq)",factory.GetPattern(null,f));
        }
        [Test]
        public void OverlappingMatches_OffsetOverlaps()
        {
            Failure f = new Failure(new[]
                {
                    new FailurePart("req",FailureClassification.Person,1),
                    new FailurePart("q",FailureClassification.Organization,3),
                })
                {ProblemValue = "Frequent Problems"};

            var factory = new MatchProblemValuesPatternFactory();
            
            //fallback onto full match because of overlapping problem words
            Assert.AreEqual("^Frequent\\ Problems$",factory.GetPattern(null,f));
        }

        [Test]
        public void OverlappingMatches_NoOverlaps()
        {
            Failure f = new Failure(new[]
                {
                    new FailurePart("re",FailureClassification.Person,1),
                    new FailurePart("quent",FailureClassification.Organization,3),
                })
                {ProblemValue = "Frequent Problems"};

            var factory = new MatchProblemValuesPatternFactory();
            Assert.AreEqual("(re).*(quent)",factory.GetPattern(null,f));
        }
    }
}