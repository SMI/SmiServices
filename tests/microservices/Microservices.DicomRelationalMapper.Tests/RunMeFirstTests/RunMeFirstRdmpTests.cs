
using FAnsi;
using NUnit.Framework;
using Smi.Common.Tests;
using System;
using System.IO;
using MapsDirectlyToDatabaseTable;
using Tests.Common;

namespace Microservices.DicomRelationalMapper.Tests.RunMeFirstTests
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

            Console.WriteLine("TestDatabases.txt");
            Console.WriteLine("----------------------------------------------------------");
            Console.Write(File.ReadAllText(f));

            Exception ex;
            if (CatalogueRepository is not ITableRepository crtr || !crtr.DiscoveredServer.RespondsWithinTime(5, out ex))
                Assert.Fail("Catalogue database was unreachable");
            if (DataExportRepository is not ITableRepository dertr || !dertr.DiscoveredServer.RespondsWithinTime(5, out ex))
                Assert.Fail("DataExport database was unreachable");

        }
    }
}
