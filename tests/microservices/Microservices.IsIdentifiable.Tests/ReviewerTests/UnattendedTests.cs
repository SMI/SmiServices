using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FAnsi.Implementation;
using FAnsi.Implementations.MicrosoftSQL;
using IsIdentifiableReviewer;
using IsIdentifiableReviewer.Out;
using NUnit.Framework;

namespace Microservices.IsIdentifiable.Tests.ReviewerTests
{
    class UnattendedTests
    {
        [Test]
        public void NoFileToProcess_Throws()
        {

            var ex = Assert.Throws<Exception>(() => new UnattendedReviewer(new IsIdentifiableReviewerOptions(),null));
            Assert.AreEqual("Unattended requires a file of errors to process",ex.Message);
        }

        [Test]
        public void NonExistantFileToProcess_Throws()
        {

            var ex = Assert.Throws<FileNotFoundException>(() => new UnattendedReviewer(new IsIdentifiableReviewerOptions()
            {
                FailuresCsv = "troll.csv"
            },null));
            StringAssert.Contains("Could not find Failures file",ex.Message);
        }
        
        [Test]
        public void NoTarget_Throws()
        {

            var fi = Path.Combine(TestContext.CurrentContext.WorkDirectory, "myfile.txt");
            File.WriteAllText(fi,"fff");
            
            var ex = Assert.Throws<Exception>(() => new UnattendedReviewer(new IsIdentifiableReviewerOptions()
            {
                FailuresCsv = fi
            },null));
            StringAssert.Contains("A single Target must be supplied for database updates",ex.Message);
        }

        [Test]
        public void NoOutputPath_Throws()
        {
            var fi = Path.Combine(TestContext.CurrentContext.WorkDirectory, "myfile.txt");
            File.WriteAllText(fi,"fff");
            
            var ex = Assert.Throws<Exception>(() => new UnattendedReviewer(new IsIdentifiableReviewerOptions()
            {
                FailuresCsv = fi
            },new Target()));
            StringAssert.Contains("An output path must be specified ",ex.Message);
        }

        
        [Test]
        public void Passes_NoFailures()
        {
            //the default Target() will be this DatabaseType
            ImplementationManager.Load<MicrosoftSQLImplementation>();

            var fi = Path.Combine(TestContext.CurrentContext.WorkDirectory, "myfile.csv");
            File.WriteAllText(fi,"fff");

            var fiOut = Path.Combine(TestContext.CurrentContext.WorkDirectory, "out.csv");

            var reviewer = new UnattendedReviewer(new IsIdentifiableReviewerOptions()
            {
                FailuresCsv = fi,
                UnattendedOutputPath = fiOut
            }, new Target());

            Assert.AreEqual(0,reviewer.Run());
            
            //just the headers
            StringAssert.AreEqualIgnoringCase("Resource,ResourcePrimaryKey,ProblemField,ProblemValue,PartWords,PartClassifications,PartOffsets",File.ReadAllText(fiOut).TrimEnd());
        }

        [Test]
        public void Passes_FailuresAllUnprocessed()
        {
            //the default Target() will be this DatabaseType
            ImplementationManager.Load<MicrosoftSQLImplementation>();

            var inputFile = @"Resource,ResourcePrimaryKey,ProblemField,ProblemValue,PartWords,PartClassifications,PartOffsets
FunBooks.HappyOzz,1.2.3,Narrative,We aren't in Kansas anymore Toto,Kansas###Toto,Location###Location,13###28";

            var fi = Path.Combine(TestContext.CurrentContext.WorkDirectory, "myfile.csv");
            File.WriteAllText(fi,inputFile);

            var fiOut = Path.Combine(TestContext.CurrentContext.WorkDirectory, "out.csv");
            
            //cleanup any remnant whitelist or redlists
            var fiWhitelist = Path.Combine(TestContext.CurrentContext.WorkDirectory,IgnoreRuleGenerator.DefaultFileName);
            var fiRedlist = Path.Combine(TestContext.CurrentContext.WorkDirectory, RowUpdater.DefaultFileName);

            if(File.Exists(fiWhitelist))
                File.Delete(fiWhitelist);
            
            if(File.Exists(fiRedlist))
                File.Delete(fiRedlist);
            
            var reviewer = new UnattendedReviewer(new IsIdentifiableReviewerOptions()
            {
                FailuresCsv = fi,
                UnattendedOutputPath = fiOut
            }, new Target());

            Assert.AreEqual(0,reviewer.Run());
            
            //all that we put in is unprocessed so should come out the same
            StringAssert.AreEqualIgnoringCase(inputFile,File.ReadAllText(fiOut).TrimEnd());

            Assert.AreEqual(1,reviewer.Total);
            Assert.AreEqual(0,reviewer.Ignores);
            Assert.AreEqual(1,reviewer.Unresolved);
            Assert.AreEqual(0,reviewer.Updates);
        }
        
        [Test]
        public void Passes_FailuresAllIgnored()
        {
            //the default Target() will be this DatabaseType
            ImplementationManager.Load<MicrosoftSQLImplementation>();

            var inputFile = @"Resource,ResourcePrimaryKey,ProblemField,ProblemValue,PartWords,PartClassifications,PartOffsets
FunBooks.HappyOzz,1.2.3,Narrative,We aren't in Kansas anymore Toto,Kansas###Toto,Location###Location,13###28";

            var fi = Path.Combine(TestContext.CurrentContext.WorkDirectory, "myfile.csv");
            File.WriteAllText(fi,inputFile);

            var fiOut = Path.Combine(TestContext.CurrentContext.WorkDirectory, "out.csv");
            
            //cleanup any remnant whitelist or redlists
            var fiWhitelist = Path.Combine(TestContext.CurrentContext.WorkDirectory,IgnoreRuleGenerator.DefaultFileName);
            var fiRedlist = Path.Combine(TestContext.CurrentContext.WorkDirectory, RowUpdater.DefaultFileName);

            if(File.Exists(fiWhitelist))
                File.Delete(fiWhitelist);
            
            if(File.Exists(fiRedlist))
                File.Delete(fiRedlist);

            //add a whitelist to ignore these
            File.WriteAllText(fiWhitelist,
                @"
- Action: Ignore
  IfColumn: Narrative
  IfPattern: ^We\ aren't\ in\ Kansas\ anymore\ Toto$");
            
            var reviewer = new UnattendedReviewer(new IsIdentifiableReviewerOptions()
            {
                FailuresCsv = fi,
                UnattendedOutputPath = fiOut
            }, new Target());

            Assert.AreEqual(0,reviewer.Run());
            
            //headers only since whitelist eats the rest
            StringAssert.AreEqualIgnoringCase(@"Resource,ResourcePrimaryKey,ProblemField,ProblemValue,PartWords,PartClassifications,PartOffsets",File.ReadAllText(fiOut).TrimEnd());

            Assert.AreEqual(1,reviewer.Total);
            Assert.AreEqual(1,reviewer.Ignores);
            Assert.AreEqual(0,reviewer.Unresolved);
            Assert.AreEqual(0,reviewer.Updates);
        }
    }
}
