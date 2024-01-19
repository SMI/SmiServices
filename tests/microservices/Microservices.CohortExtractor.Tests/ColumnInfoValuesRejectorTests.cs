using FAnsi;
using FAnsi.Discovery;
using Microservices.CohortExtractor.Execution.RequestFulfillers;
using Moq;
using NUnit.Framework;
using Rdmp.Core.Curation;
using Rdmp.Core.Curation.Data;
using System.Collections.Generic;
using System.Data.Common;
using Tests.Common;
using Smi.Common.Tests;

namespace Microservices.CohortExtractor.Tests
{

    [RequiresRelationalDb(DatabaseType.MicrosoftSQLServer)]
    [RequiresRelationalDb(DatabaseType.MySql)]
    public class ColumnInfoValuesRejectorTests : DatabaseTests
    {
        private const string PatColName = "PatientID";

        [TestCase(DatabaseType.MicrosoftSQLServer)]
        [TestCase(DatabaseType.MySql)]
        public void Test_ColumnInfoValuesRejectorTests(DatabaseType type)
        {
            DiscoveredDatabase server = GetCleanedServer(type);
            DiscoveredTable tbl = server.CreateTable("BadPatients", new[] { new DatabaseColumnRequest(PatColName, "varchar(100)") });

            tbl.Insert(new Dictionary<string, object> { { PatColName, "Frank" } });
            tbl.Insert(new Dictionary<string, object> { { PatColName, "Peter" } });
            tbl.Insert(new Dictionary<string, object> { { PatColName, "Frank" } }); //duplication for the lols
            tbl.Insert(new Dictionary<string, object> { { PatColName, "David" } });

            new TableInfoImporter(CatalogueRepository, tbl).DoImport(out var _, out ColumnInfo[] cols);

            var rejector = new ColumnInfoValuesRejector(cols[0]);

            var moqDave = new Mock<DbDataReader>();
            moqDave.Setup(x => x[PatColName])
                .Returns("Dave");

            Assert.IsFalse(rejector.Reject(moqDave.Object, out string? reason));
            Assert.IsNull(reason);

            var moqFrank = new Mock<DbDataReader>();
            moqFrank.Setup(x => x[PatColName])
                .Returns("Frank");

            Assert.IsTrue(rejector.Reject(moqFrank.Object, out reason));
            Assert.AreEqual("Patient or Identifier was in reject list", reason);

            var moqLowerCaseFrank = new Mock<DbDataReader>();
            moqLowerCaseFrank.Setup(x => x[PatColName])
                .Returns("frank");

            Assert.IsTrue(rejector.Reject(moqLowerCaseFrank.Object, out reason));
            Assert.AreEqual("Patient or Identifier was in reject list", reason);
        }

    }
}
