using System;
using IsIdentifiableReviewer.Out;
using Microservices.IsIdentifiable.Failures;
using Microservices.IsIdentifiable.Reporting;
using NUnit.Framework;

namespace Microservices.IsIdentifiable.Tests.ReviewerTests
{
    public class SymbolsRulesFactoryTests
    {
        
        [TestCase("MR Head 12-11-20","12-11-20",@"(\d\d-\d\d-\d\d)$",SymbolsRuleFactoryMode.Full)]
        [TestCase("CT Head - 12/34/56","12/34/56",@"(\d\d/\d\d/\d\d)$",SymbolsRuleFactoryMode.Full)]
        [TestCase("CT Head - 123-ABC-n4 fishfish","123-ABC-n4",@"(\d\d\d-[A-Z][A-Z][A-Z]-[a-z]\d)",SymbolsRuleFactoryMode.Full)]
        [TestCase("CT Head - 123-ABC-n4 fishfish","123-ABC-n4",@"(123-[A-Z][A-Z][A-Z]-[a-z]4)",SymbolsRuleFactoryMode.CharactersOnly)]
        [TestCase("CT Head - 123-ABC-n4 fishfish","123-ABC-n4",@"(\d\d\d-ABC-n\d)",SymbolsRuleFactoryMode.DigitsOnly)]
        [TestCase("123","123",@"^(\d\d\d)$",SymbolsRuleFactoryMode.Full)]
        public void TestSymbols_OnePart(string input,string part, string expectedOutput,SymbolsRuleFactoryMode mode)
        {
            var f = new SymbolsRulesFactory(){Mode = mode};

            var failure = new Failure(new[] {new FailurePart(part, FailureClassification.Person, input.IndexOf(part))})
            {
                ProblemValue = input
            };

            Assert.AreEqual(expectedOutput,f.GetPattern(this, failure));
        }


        [TestCase("12 Morton Street","12","eet",@"^(\d\d).*([a-z][a-z][a-z])$")]
        [TestCase("Morton MR Smith","MR","Smith",@"([A-Z][A-Z]).*([A-Z][a-z][a-z][a-z][a-z])$")]
        public void TestSymbols_TwoParts_NoOverlap(string input,string part1,string part2, string expectedOutput)
        {
            var f = new SymbolsRulesFactory();

            var failure = new Failure(new[]
            {
                new FailurePart(part1, FailureClassification.Person, input.IndexOf(part1)),
                new FailurePart(part2, FailureClassification.Person, input.IndexOf(part2))
            })
            {
                ProblemValue = input
            };

            Assert.AreEqual(expectedOutput,f.GetPattern(this, failure));
        }

        [TestCase("Theres a candy coloured clown they call the Sandman", "clown they", "they call", @"([a-z][a-z][a-z][a-z][a-z]\ [a-z][a-z][a-z][a-z]\ [a-z][a-z][a-z][a-z])$")]
        [TestCase("Clowns","Cl","lowns",@"^([A-Z][a-z][a-z][a-z][a-z][a-z])$")]
        public void TestSymbols_TwoParts_Overlap(string input,string part1,string part2, string expectedOutput)
        {
            var f = new SymbolsRulesFactory();

            var failure = new Failure(new[]
            {
                new FailurePart(part1, FailureClassification.Person, input.IndexOf(part1)),
                new FailurePart(part2, FailureClassification.Person, input.IndexOf(part2))
            })
            {
                ProblemValue = input
            };

            Assert.AreEqual(expectedOutput,f.GetPattern(this, failure));
        }
        [Test]
        public void TestNoParts()
        {
            var f = new SymbolsRulesFactory();
            var ex = Assert.Throws<ArgumentException>(()=> f.GetPattern(this, new Failure(new FailurePart[0]) {ProblemValue = "fdslkfl;asdf"}));
            Assert.AreEqual("Failure had no Parts",ex.Message);

        }
    }
}