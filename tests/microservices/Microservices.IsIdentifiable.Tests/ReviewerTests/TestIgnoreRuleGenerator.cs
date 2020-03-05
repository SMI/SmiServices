using System.IO;
using IsIdentifiableReviewer.Out;
using Microservices.IsIdentifiable.Failures;
using NUnit.Framework;

namespace Microservices.IsIdentifiable.Tests.ReviewerTests
{
    class TestIgnoreRuleGenerator
    {
        [Test]
        public void TestRepeatedIgnoring()
        {
            var failure = new Reporting.Failure(
                new FailurePart[]
                {
                    new FailurePart("Kansas", FailureClassification.Location, 13),
                    new FailurePart("Toto", FailureClassification.Location, 28)
                })
            {
                ProblemValue = "We aren't in Kansas anymore Toto",
                ProblemField = "Narrative",
                ResourcePrimaryKey = "1.2.3.4"
            };

            var newRules = new FileInfo(Path.Combine(TestContext.CurrentContext.WorkDirectory, "IgnoreList.yaml"));

            //make sure repeat test runs work properly
            if(File.Exists(newRules.FullName))
                File.Delete(newRules.FullName);

            IgnoreRuleGenerator updater = new IgnoreRuleGenerator(newRules);

            //it should be novel i.e. require user decision
            Assert.IsTrue(updater.OnLoad(failure));

            //we tell it to ignore this value
            updater.Add(failure);
            
            Assert.AreEqual(
                @"- Action: Ignore
  IfColumn: Narrative
  IfPattern: ^We\ aren't\ in\ Kansas\ anymore\ Toto$
",File.ReadAllText(newRules.FullName)); //btw slash space is a 'literal space' so legit

            //it should be no longer be novel
            Assert.IsFalse(updater.OnLoad(failure));

        }
    }
}