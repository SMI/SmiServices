using FAnsi;
using FAnsi.Discovery;
using Moq;
using NUnit.Framework;
using Rdmp.Core.Curation;
using Rdmp.Core.Curation.Data;
using SmiServices.Microservices.CohortExtractor.RequestFulfillers;
using SmiServices.UnitTests.Common;
using System.Collections.Generic;
using System.Data.Common;
using Tests.Common;

namespace SmiServices.UnitTests.Microservices.CohortExtractor
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

            Assert.Multiple(() =>
            {
                Assert.That(rejector.Reject(moqDave.Object, out string? reason), Is.False);
                Assert.That(reason, Is.Null);
            });

            var moqFrank = new Mock<DbDataReader>();
            moqFrank.Setup(x => x[PatColName])
                .Returns("Frank");

            Assert.Multiple(() =>
            {
                Assert.That(rejector.Reject(moqFrank.Object, out var reason), Is.True);
                Assert.That(reason, Is.EqualTo("Patient or Identifier was in reject list"));
            });

            var moqLowerCaseFrank = new Mock<DbDataReader>();
            moqLowerCaseFrank.Setup(x => x[PatColName])
                .Returns("frank");

            Assert.Multiple(() =>
            {
                Assert.That(rejector.Reject(moqLowerCaseFrank.Object, out var reason), Is.True);
                Assert.That(reason, Is.EqualTo("Patient or Identifier was in reject list"));
            });
        }

    }
}
