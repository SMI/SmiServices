using System.Data;
using FAnsi;
using Microservices.CohortExtractor.Execution.RequestFulfillers;
using NUnit.Framework;
using Tests.Common;
using Smi.Common.Tests;

namespace Microservices.CohortExtractor.Tests
{
    [RequiresRelationalDb(DatabaseType.MicrosoftSQLServer)]
    [RequiresRelationalDb(DatabaseType.MySql)]
    class BlacklistRejectorTests : DatabaseTests
    {
        [TestCase(DatabaseType.MicrosoftSQLServer)]
        [TestCase(DatabaseType.MySql)]
        [TestCase(DatabaseType.PostgreSql)]
        [TestCase(DatabaseType.Oracle)]
        public void TestBlacklistOn_Study(DatabaseType dbType)
        {
            var db = GetCleanedServer(dbType);

            var dt = new DataTable();
            dt.Columns.Add("StudyInstanceUID");
            dt.Rows.Add("fff");

            var tbl = db.CreateTable("SomeTbl", dt);

            var cata = Import(tbl);

            var rejector = new BlacklistRejector(cata);
            Assert.IsTrue(rejector.DoLookup("fff", "aaa", "bbb"));
            Assert.IsFalse(rejector.DoLookup("aaa","fff", "bbb"));
            Assert.IsFalse(rejector.DoLookup("aaa","bbb","fff"));
        }


        [TestCase(DatabaseType.MicrosoftSQLServer)]
        [TestCase(DatabaseType.MySql)]
        [TestCase(DatabaseType.PostgreSql)]
        [TestCase(DatabaseType.Oracle)]
        public void TestBlacklistOn_Series(DatabaseType dbType)
        {
            var db = GetCleanedServer(dbType);

            var dt = new DataTable();
            dt.Columns.Add("SeriesInstanceUID");
            dt.Rows.Add("fff");

            var tbl = db.CreateTable("SomeTbl", dt);

            var cata = Import(tbl);

            var rejector = new BlacklistRejector(cata);
            Assert.IsFalse(rejector.DoLookup("fff", "aaa", "bbb"));
            Assert.IsTrue(rejector.DoLookup("aaa","fff", "bbb"));
            Assert.IsFalse(rejector.DoLookup("aaa","bbb","fff"));
        }


        [TestCase(DatabaseType.MicrosoftSQLServer)]
        [TestCase(DatabaseType.MySql)]
        [TestCase(DatabaseType.PostgreSql)]
        [TestCase(DatabaseType.Oracle)]
        public void TestBlacklistOn_SOPInstanceUID(DatabaseType dbType)
        {
            var db = GetCleanedServer(dbType);

            var dt = new DataTable();
            dt.Columns.Add("SOPInstanceUID");
            dt.Rows.Add("fff");

            var tbl = db.CreateTable("SomeTbl", dt);

            var cata = Import(tbl);

            var rejector = new BlacklistRejector(cata);
            Assert.IsFalse(rejector.DoLookup("fff", "aaa", "bbb"));
            Assert.IsFalse(rejector.DoLookup("aaa","fff", "bbb"));
            Assert.IsTrue(rejector.DoLookup("aaa","bbb","fff"));
        }

        
        [TestCase(DatabaseType.MicrosoftSQLServer)]
        [TestCase(DatabaseType.MySql)]
        [TestCase(DatabaseType.PostgreSql)]
        [TestCase(DatabaseType.Oracle)]
        public void TestBlacklistOn_AllThree(DatabaseType dbType)
        {
            var db = GetCleanedServer(dbType);

            var dt = new DataTable();
            dt.Columns.Add("StudyInstanceUID");
            dt.Columns.Add("SeriesInstanceUID");
            dt.Columns.Add("SOPInstanceUID");
            dt.Columns.Add("SomeOtherCol");

            dt.Rows.Add("aaa","bbb","ccc","ffff");

            var tbl = db.CreateTable("SomeTbl", dt);

            var cata = Import(tbl);

            var rejector = new BlacklistRejector(cata);
            Assert.IsTrue(rejector.DoLookup("aaa","bbb","ccc"));
            Assert.IsTrue(rejector.DoLookup("---","bbb","ccc"));
            Assert.IsTrue(rejector.DoLookup("aaa","bbb","---"));
            Assert.IsTrue(rejector.DoLookup("---","bbb","---"));
            Assert.IsTrue(rejector.DoLookup("---","---","ccc"));
            Assert.IsTrue(rejector.DoLookup("aaa","---","---"));
            
            Assert.IsFalse(rejector.DoLookup("---","---","---"));
            Assert.IsFalse(rejector.DoLookup("bbb","ccc","aaa"));
            Assert.IsFalse(rejector.DoLookup("---","ccc","bbb"));
            Assert.IsFalse(rejector.DoLookup("bbb","aaa","---"));
            Assert.IsFalse(rejector.DoLookup("---","aaa","---"));
            Assert.IsFalse(rejector.DoLookup("---","---","bbb"));
            Assert.IsFalse(rejector.DoLookup("bbb","---","---"));
        }
    }
}
