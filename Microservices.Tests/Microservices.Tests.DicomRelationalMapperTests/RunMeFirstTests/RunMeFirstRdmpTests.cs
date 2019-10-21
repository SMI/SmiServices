using System;
using System.IO;
using FAnsi;
using NUnit.Framework;
using Tests.Common;
using Tests.Common.Smi;

namespace Microservices.Tests.RDMPTests.RunMeFirstTests
{
    [RequiresRelationalDb(DatabaseType.MicrosoftSQLServer)]
    [Category("RunMeFirst")]
    public class RunMeFirstRdmpTests:DatabaseTests
    {
        [Test]
        public void PlatformDatabasesAvailable()
        {
            var f = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestDatabases.txt");

            if(!File.Exists(f))
                Assert.Fail("TestDatabases.txt was not found in the bin directory, check the project includes a reference to HIC.RDMP.Plugin.Tests nuget package and that the file is set to CopyAlways");
            
            Console.WriteLine("TestDatabases.txt");
            Console.WriteLine("----------------------------------------------------------");
            Console.Write(File.ReadAllText(f));

            Exception ex;
            if(!CatalogueRepository.DiscoveredServer.RespondsWithinTime(5, out ex))
                Assert.Fail("Catalogue database was unreachable");
            if (!DataExportRepository.DiscoveredServer.RespondsWithinTime(5, out ex))
                Assert.Fail("DataExport database was unreachable");

        }
    }
}
