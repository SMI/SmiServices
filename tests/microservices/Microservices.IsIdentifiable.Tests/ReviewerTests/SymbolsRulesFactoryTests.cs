using IsIdentifiableReviewer.Out;
using Microservices.IsIdentifiable.Failures;
using Microservices.IsIdentifiable.Reporting;
using NUnit.Framework;

namespace Microservices.IsIdentifiable.Tests.ReviewerTests
{
    public class SymbolsRulesFactoryTests
    {

        [TestCase("12/34/56",@"^\d\d/\d\d/\d\d$")]
        
        [TestCase("1 6",@"^\d\ \d$")]

        [TestCase("abc\n123",@"^[a-z][a-z][a-z]\n\d\d\d$")]
        public void TestSymbols(string input, string expectedOutput)
        {
            var f = new SymbolsRulesFactory();

            Assert.AreEqual(
                expectedOutput,
                f.GetPattern(this, new Failure(new FailurePart[0]) {ProblemValue = input})
                );
        }
    }
}