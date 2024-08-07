using FAnsi;
using NUnit.Framework;
using Rdmp.Core.MapsDirectlyToDatabaseTable;
using SmiServices.IntegrationTests;
using SmiServices.UnitTests.Common;
using System.IO;
using Tests.Common;

namespace SmiServices.UnitTests.Microservices.DicomRelationalMapper.RunMeFirstTests
{
    [RequiresRelationalDb(DatabaseType.MicrosoftSQLServer)]
    [Category("RunMeFirst")]
    public class RunMeFirstRdmpTests : DatabaseTests
    {
        [Test]
        public void PlatformDatabasesAvailable()
        {
            var f = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestDatabases.txt");

            if (!File.Exists(f))
                Assert.Fail("TestDatabases.txt was not found in the bin directory, check the project includes a reference to HIC.RDMP.Plugin.Tests nuget package and that the file is set to CopyAlways");

            if (CatalogueRepository is ITableRepository crtr && !crtr.DiscoveredServer.RespondsWithinTime(5, out _))
                Assert.Fail("Catalogue database was unreachable");
            if (DataExportRepository is ITableRepository dertr && !dertr.DiscoveredServer.RespondsWithinTime(5, out _))
                Assert.Fail("DataExport database was unreachable");
        }
    }
}
