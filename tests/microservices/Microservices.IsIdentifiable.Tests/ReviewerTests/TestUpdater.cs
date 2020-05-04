using System.Data;
using System.IO;
using FAnsi;
using IsIdentifiableReviewer.Out;
using IsIdentifiableReviewer.Out.UpdateStrategies;
using Microservices.IsIdentifiable.Failures;
using Microservices.IsIdentifiable.Reporting;
using Microservices.IsIdentifiable.Rules;
using Moq;
using NUnit.Framework;
using Smi.Common.Tests;
using Tests.Common;

namespace Microservices.IsIdentifiable.Tests.ReviewerTests
{
    class TestUpdater : DatabaseTests
    {
        [TestCase(DatabaseType.MySql)]
        [TestCase(DatabaseType.MicrosoftSQLServer)]
        [TestCase(DatabaseType.Oracle)]
        [TestCase(DatabaseType.PostgreSql)]
        public void Test(DatabaseType dbType)
        {
            var db = GetCleanedServer(dbType);
            var dbname = db.GetRuntimeName();

            var failure = new Reporting.Failure(
                new FailurePart[]
                {
                    new FailurePart("Kansas", FailureClassification.Location, 13),
                    new FailurePart("Toto", FailureClassification.Location, 28)
                })
            {
                ProblemValue = "We aren't in Kansas anymore Toto",
                ProblemField = "Narrative",
                ResourcePrimaryKey = "1.2.3.4",
                Resource = dbname + ".HappyOzz"
            };

            DataTable dt = new DataTable();
            dt.Columns.Add("MyPk");
            dt.Columns.Add("Narrative");
            
            dt.PrimaryKey = new[] {dt.Columns["MyPk"]};
            
            dt.Rows.Add("1.2.3.4", "We aren't in Kansas anymore Toto");

            var tbl = db.CreateTable("HappyOzz",dt);

            //redacted string will be longer! 
            var col = tbl.DiscoverColumn("Narrative");
            col.DataType.Resize(1000);

            var newRules = new FileInfo(Path.Combine(TestContext.CurrentContext.WorkDirectory, "Redlist.yaml"));

            //make sure repeat test runs work properly
            if(File.Exists(newRules.FullName))
                File.Delete(newRules.FullName);

            RowUpdater updater = new RowUpdater(newRules);
            updater.UpdateStrategy = new ProblemValuesUpdateStrategy();

            //it should be novel i.e. require user decision
            Assert.IsTrue(updater.OnLoad(db.Server,failure, out _));

            updater.Update(db.Server,failure,null);

            var result = tbl.GetDataTable();
            Assert.AreEqual("We aren't in SMI_REDACTED anymore SMI_REDACTED",result.Rows[0]["Narrative"]);

            TestHelpers.Contains(
@"- Action: Report
  IfColumn: Narrative
  As: Location
  IfPattern: ^We\ aren't\ in\ Kansas\ anymore\ Toto$
",File.ReadAllText(newRules.FullName)); //btw slash space is a 'literal space' so legit

            //it should be updated automatically and not require user decision
            Assert.IsFalse(updater.OnLoad(db.Server,failure,out _));
            
        }


        [TestCase(DatabaseType.MySql,true)]
        [TestCase(DatabaseType.MySql,false)]
        [TestCase(DatabaseType.MicrosoftSQLServer,true)]
        [TestCase(DatabaseType.MicrosoftSQLServer,false)]
        [TestCase(DatabaseType.Oracle,true)]
        [TestCase(DatabaseType.Oracle,false)]
        [TestCase(DatabaseType.PostgreSql,true)]
        [TestCase(DatabaseType.PostgreSql,false)]
        public void Test_RegexUpdateStrategy(DatabaseType dbType, bool provideCaptureGroup)
        {
            var db = GetCleanedServer(dbType);
            var dbname = db.GetRuntimeName();

            //the Failure was about Kansas and Toto
            var failure = new Reporting.Failure(
                new FailurePart[]
                {
                    new FailurePart("Kansas", FailureClassification.Location, 13),
                    new FailurePart("Toto", FailureClassification.Location, 28)
                })
            {
                ProblemValue = "We aren't in Kansas anymore Toto",
                ProblemField = "Narrative",
                ResourcePrimaryKey = "1.2.3.4",
                Resource = dbname + ".HappyOzz"
            };
            
            DataTable dt = new DataTable();
            dt.Columns.Add("MyPk");
            dt.Columns.Add("Narrative");
            
            dt.PrimaryKey = new[] {dt.Columns["MyPk"]};
            
            dt.Rows.Add("1.2.3.4", "We aren't in Kansas anymore Toto");

            var tbl = db.CreateTable("HappyOzz",dt);

            //redacted string will be longer! 
            var col = tbl.DiscoverColumn("Narrative");
            col.DataType.Resize(1000);

            var newRules = new FileInfo(Path.Combine(TestContext.CurrentContext.WorkDirectory, "Redlist.yaml"));

            //make sure repeat test runs work properly
            if(File.Exists(newRules.FullName))
                File.Delete(newRules.FullName);

            RowUpdater updater = new RowUpdater(newRules);
            
            //But the user told us that only the Toto bit is a problem
            string rule = provideCaptureGroup ? "(Toto)$" : "Toto$";
            updater.RulesFactory = Mock.Of<IRulePatternFactory>(m=>m.GetPattern(It.IsAny<object>(),It.IsAny<Failure>()) == rule); 

            //this is the thing we are actually testing, where we update based on the usersRule not the failing parts
            updater.UpdateStrategy = new RegexUpdateStrategy();
            
            //it should be novel i.e. require user decision
            Assert.IsTrue(updater.OnLoad(db.Server,failure,out _));

            updater.Update(db.Server,failure,null);//<- null here will trigger the rule pattern factory to prompt 'user' for pattern which is "(Toto)$"

            var result = tbl.GetDataTable();

            if(provideCaptureGroup)
                Assert.AreEqual("We aren't in Kansas anymore SMI_REDACTED",result.Rows[0]["Narrative"],"Expected update to only affect the capture group ToTo");
            else
                Assert.AreEqual("We aren't in SMI_REDACTED anymore SMI_REDACTED", result.Rows[0]["Narrative"],"Because regex had no capture group we expected the update strategy to fallback on Failure Part matching");

            //it should be updated automatically and not require user decision
            Assert.IsFalse(updater.OnLoad(db.Server,failure,out _));
            
        }
    }
}
