using FAnsi;
using NUnit.Framework;
using SmiServices.IntegrationTests;
using SmiServices.Microservices.CohortExtractor.RequestFulfillers;
using SmiServices.UnitTests.Common;
using System.Data;
using Tests.Common;

namespace SmiServices.UnitTests.Microservices.CohortExtractor
{
    [RequiresRelationalDb(DatabaseType.MicrosoftSQLServer)]
    [RequiresRelationalDb(DatabaseType.MySql)]
    [RequiresRelationalDb(DatabaseType.PostgreSql)]
    class BlacklistRejectorTests : DatabaseTests
    {
        [TestCase(DatabaseType.MicrosoftSQLServer)]
        [TestCase(DatabaseType.MySql)]
        [TestCase(DatabaseType.PostgreSql)]
        public void TestBlacklistOn_Study(DatabaseType dbType)
        {
            var db = GetCleanedServer(dbType);

            using var dt = new DataTable();
            dt.Columns.Add("StudyInstanceUID");
            dt.Rows.Add("fff");

            var tbl = db.CreateTable("SomeTbl", dt);

            var cata = Import(tbl);

            var rejector = new BlacklistRejector(cata);
            Assert.Multiple(() =>
            {
                Assert.That(rejector.DoLookup("fff", "aaa", "bbb"), Is.True);
                Assert.That(rejector.DoLookup("aaa", "fff", "bbb"), Is.False);
                Assert.That(rejector.DoLookup("aaa", "bbb", "fff"), Is.False);
            });
        }


        [TestCase(DatabaseType.MicrosoftSQLServer)]
        [TestCase(DatabaseType.MySql)]
        [TestCase(DatabaseType.PostgreSql)]
        public void TestBlacklistOn_Series(DatabaseType dbType)
        {
            var db = GetCleanedServer(dbType);

            using var dt = new DataTable();
            dt.Columns.Add("SeriesInstanceUID");
            dt.Rows.Add("fff");

            var tbl = db.CreateTable("SomeTbl", dt);

            var cata = Import(tbl);

            var rejector = new BlacklistRejector(cata);
            Assert.Multiple(() =>
            {
                Assert.That(rejector.DoLookup("fff", "aaa", "bbb"), Is.False);
                Assert.That(rejector.DoLookup("aaa", "fff", "bbb"), Is.True);
                Assert.That(rejector.DoLookup("aaa", "bbb", "fff"), Is.False);
            });
        }


        [TestCase(DatabaseType.MicrosoftSQLServer)]
        [TestCase(DatabaseType.MySql)]
        [TestCase(DatabaseType.PostgreSql)]
        public void TestBlacklistOn_SOPInstanceUID(DatabaseType dbType)
        {
            var db = GetCleanedServer(dbType);

            using var dt = new DataTable();
            dt.Columns.Add("SOPInstanceUID");
            dt.Rows.Add("fff");

            var tbl = db.CreateTable("SomeTbl", dt);

            var cata = Import(tbl);

            var rejector = new BlacklistRejector(cata);
            Assert.Multiple(() =>
            {
                Assert.That(rejector.DoLookup("fff", "aaa", "bbb"), Is.False);
                Assert.That(rejector.DoLookup("aaa", "fff", "bbb"), Is.False);
                Assert.That(rejector.DoLookup("aaa", "bbb", "fff"), Is.True);
            });
        }


        [TestCase(DatabaseType.MicrosoftSQLServer)]
        [TestCase(DatabaseType.MySql)]
        [TestCase(DatabaseType.PostgreSql)]
        public void TestBlacklistOn_AllThree(DatabaseType dbType)
        {
            var db = GetCleanedServer(dbType);

            using var dt = new DataTable();
            dt.Columns.Add("StudyInstanceUID");
            dt.Columns.Add("SeriesInstanceUID");
            dt.Columns.Add("SOPInstanceUID");
            dt.Columns.Add("SomeOtherCol");

            dt.Rows.Add("aaa", "bbb", "ccc", "ffff");

            var tbl = db.CreateTable("SomeTbl", dt);

            var cata = Import(tbl);

            var rejector = new BlacklistRejector(cata);
            Assert.Multiple(() =>
            {
                Assert.That(rejector.DoLookup("aaa", "bbb", "ccc"), Is.True);
                Assert.That(rejector.DoLookup("---", "bbb", "ccc"), Is.True);
                Assert.That(rejector.DoLookup("aaa", "bbb", "---"), Is.True);
                Assert.That(rejector.DoLookup("---", "bbb", "---"), Is.True);
                Assert.That(rejector.DoLookup("---", "---", "ccc"), Is.True);
                Assert.That(rejector.DoLookup("aaa", "---", "---"), Is.True);

                Assert.That(rejector.DoLookup("---", "---", "---"), Is.False);
                Assert.That(rejector.DoLookup("bbb", "ccc", "aaa"), Is.False);
                Assert.That(rejector.DoLookup("---", "ccc", "bbb"), Is.False);
                Assert.That(rejector.DoLookup("bbb", "aaa", "---"), Is.False);
                Assert.That(rejector.DoLookup("---", "aaa", "---"), Is.False);
                Assert.That(rejector.DoLookup("---", "---", "bbb"), Is.False);
                Assert.That(rejector.DoLookup("bbb", "---", "---"), Is.False);
            });
        }
    }
}
