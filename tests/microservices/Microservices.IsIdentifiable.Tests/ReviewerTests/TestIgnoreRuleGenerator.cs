using System.IO;
using System.Linq;
using IsIdentifiableReviewer.Out;
using Microservices.IsIdentifiable.Failures;
using Microservices.IsIdentifiable.Reporting;
using NUnit.Framework;
using Smi.Common.Tests;

namespace Microservices.IsIdentifiable.Tests.ReviewerTests
{
    class TestIgnoreRuleGenerator
    {
        [Test]
        public void TestRepeatedIgnoring()
        {
            var failure = new Failure(
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

            IgnoreRuleGenerator ignorer = new IgnoreRuleGenerator(newRules);

            //it should be novel i.e. require user decision
            Assert.IsTrue(ignorer.OnLoad(failure,out _));

            //we tell it to ignore this value
            ignorer.Add(failure);
            
            TestHelpers.Contains(
                @"- Action: Ignore
  IfColumn: Narrative
  IfPattern: ^We\ aren't\ in\ Kansas\ anymore\ Toto$
",File.ReadAllText(newRules.FullName)); //btw slash space is a 'literal space' so legit

            //it should be no longer be novel
            Assert.IsFalse(ignorer.OnLoad(failure, out _));

        }


        [Test]
        public void Test_SaveYaml()
        {
            var failure1 = new Failure(
                new FailurePart[]
                {
                    new FailurePart("Hadock", FailureClassification.Location, 0),
                })
            {
                ProblemValue = "Hadock",
                ProblemField = "Narrative",
                ResourcePrimaryKey = "1.2.3.4"
            };
            var failure2 = new Failure(
                new FailurePart[]
                {
                    new FailurePart("Bass", FailureClassification.Location, 0),
                })
            {
                ProblemValue = "Bass",
                ProblemField = "Narrative",
                ResourcePrimaryKey = "1.2.3.5"
            };

            var newRules = new FileInfo(Path.Combine(TestContext.CurrentContext.WorkDirectory, "IgnoreList.yaml"));

            //make sure repeat test runs work properly
            if (File.Exists(newRules.FullName))
                File.Delete(newRules.FullName);

            IgnoreRuleGenerator ignorer = new IgnoreRuleGenerator(newRules);


            //we tell it to ignore this value
            ignorer.Add(failure1);
            ignorer.Add(failure2);

            TestHelpers.Contains(
                @"
- Action: Ignore
  IfColumn: Narrative
  IfPattern: ^Hadock$".Trim(), File.ReadAllText(newRules.FullName));

            TestHelpers.Contains(
                @"
- Action: Ignore
  IfColumn: Narrative
  IfPattern: ^Bass$".Trim(), File.ReadAllText(newRules.FullName));

            ignorer.Rules.Remove(ignorer.Rules.Last());
            ignorer.Save();


            TestHelpers.Contains(
                @"
- Action: Ignore
  IfColumn: Narrative
  IfPattern: ^Hadock$".Trim(), File.ReadAllText(newRules.FullName));

            TestHelpers.DoesNotContain(
                @"
- Action: Ignore
  IfColumn: Narrative
  IfPattern: ^Bass$".Trim(), File.ReadAllText(newRules.FullName));


            ignorer.Rules.Clear();
            ignorer.Save();
            Assert.IsTrue(string.IsNullOrWhiteSpace(File.ReadAllText(newRules.FullName)));


        }

        [Test]
        public void TestUndo()
        {
            var failure = new Failure(
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

            //create an existing rule to check that Undo doesn't just nuke the entire file
            File.WriteAllText(newRules.FullName,@"- Action: Ignore
  IfColumn: Narrative
  IfPattern: ^Joker Wuz Ere$
");

            IgnoreRuleGenerator ignorer = new IgnoreRuleGenerator(newRules);

            //it should be novel i.e. require user decision
            Assert.IsTrue(ignorer.OnLoad(failure,out _));

            //we tell it to ignore this value
            ignorer.Add(failure);
            
            TestHelpers.Contains(
                @"- Action: Ignore
  IfColumn: Narrative
  IfPattern: ^We\ aren't\ in\ Kansas\ anymore\ Toto$
",File.ReadAllText(newRules.FullName)); //btw slash space is a 'literal space' so legit

            //it should be no longer be novel
            Assert.IsFalse(ignorer.OnLoad(failure, out _));

            //Undo
            Assert.AreEqual(1,ignorer.History.Count);
            Assert.AreEqual(2,ignorer.Rules.Count);
            ignorer.Undo();

            Assert.AreEqual(0,ignorer.History.Count);
            Assert.AreEqual(1,ignorer.Rules.Count);

            //only the original one should be there
            Assert.AreEqual(@"- Action: Ignore
  IfColumn: Narrative
  IfPattern: ^Joker Wuz Ere$
",File.ReadAllText(newRules.FullName));

            //repeated undo calls do nothing
            ignorer.Undo();
            ignorer.Undo();
            ignorer.Undo();
        }

        
        [Test]
        public void Test_DeleteRule()
        {
            var failure = new Failure(
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

            //create an existing rule to check that Undo doesn't just nuke the entire file
            File.WriteAllText(newRules.FullName,@"- Action: Ignore
  IfColumn: Narrative
  IfPattern: ^Joker Wuz Ere$
");

            IgnoreRuleGenerator ignorer = new IgnoreRuleGenerator(newRules);

            //it should be novel i.e. require user decision
            Assert.IsTrue(ignorer.OnLoad(failure,out _));

            //we tell it to ignore this value
            ignorer.Add(failure);
            
            TestHelpers.Contains(
                @"- Action: Ignore
  IfColumn: Narrative
  IfPattern: ^We\ aren't\ in\ Kansas\ anymore\ Toto$
",File.ReadAllText(newRules.FullName)); //btw slash space is a 'literal space' so legit

            //it should be no longer be novel
            Assert.IsFalse(ignorer.OnLoad(failure, out _));

            //Remove the last one
            Assert.AreEqual(2,ignorer.Rules.Count);
            var result = ignorer.Delete(ignorer.Rules[1]);

            Assert.IsTrue(result);

            //deleted from memory
            Assert.AreEqual(1,ignorer.Rules.Count);


            var newRulebaseYaml = File.ReadAllText(newRules.FullName);

            //only the original one should be there
            StringAssert.Contains(@"- Action: Ignore
  IfColumn: Narrative
  IfPattern: ^Joker Wuz Ere$
",newRulebaseYaml);

            StringAssert.Contains("# Rule deleted by ",newRulebaseYaml);
                       
            StringAssert.DoesNotContain("Kansas",newRulebaseYaml);

            //repeated undo calls do nothing
            ignorer.Undo();
            ignorer.Undo();
            ignorer.Undo();
        }
    }
}