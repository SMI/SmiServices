using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using FAnsi;
using IsIdentifiableReviewer.Out;
using Microservices.IsIdentifiable.Failures;
using NUnit.Framework;
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
            
            RowUpdater updater = new RowUpdater();
            updater.Update(db.Server,failure);

            var result = tbl.GetDataTable();
            Assert.AreEqual("We aren't in SMI_REDACTED anymore SMI_REDACTED",result.Rows[0]["Narrative"]);


        }
    }
}
