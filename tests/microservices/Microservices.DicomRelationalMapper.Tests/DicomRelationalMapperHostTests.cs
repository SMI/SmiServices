using FAnsi;
using Microservices.DicomRelationalMapper.Execution;
using Microservices.DicomRelationalMapper.Execution.Namers;
using NUnit.Framework;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.DataLoad;
using Smi.Common.Options;
using Smi.Common.Tests;
using System;
using System.Data;
using Rdmp.Core.MapsDirectlyToDatabaseTable;
using Tests.Common;

namespace Microservices.Tests.RDMPTests
{
    [RequiresRabbit, RequiresRelationalDb(DatabaseType.MicrosoftSQLServer)]
    public class DicomRelationalMapperHostTests : DatabaseTests
    {
        [TestCase(DatabaseType.MySql, "GuidDatabaseNamer", typeof(GuidDatabaseNamer))]
        [TestCase(DatabaseType.MicrosoftSQLServer, "GuidDatabaseNamer", typeof(GuidDatabaseNamer))]
        [TestCase(DatabaseType.MySql, "MyFixedStagingDatabaseNamer", typeof(MyFixedStagingDatabaseNamer))]
        [TestCase(DatabaseType.MicrosoftSQLServer, "MyFixedStagingDatabaseNamer", typeof(MyFixedStagingDatabaseNamer))]
        public void TestCreatingNamer_CorrectType(DatabaseType dbType, string typeName, Type expectedType)
        {
            var db = GetCleanedServer(dbType);

            var dt = new DataTable();
            dt.Columns.Add("Hi");
            dt.Rows.Add("There");

            var tbl = db.CreateTable("DicomRelationalMapperHostTests", dt);

            var cata = Import(tbl);

            var globals = new GlobalOptionsFactory().Load(nameof(TestCreatingNamer_CorrectType));
            var consumerOptions = globals.DicomRelationalMapperOptions;

            var lmd = new LoadMetadata(CatalogueRepository, "MyLoad");
            cata.LoadMetadata_ID = lmd.ID;
            cata.SaveToDatabase();

            consumerOptions!.LoadMetadataId = lmd.ID;
            consumerOptions.DatabaseNamerType = typeName;
            consumerOptions.Guid = Guid.Empty;

            if (globals.RDMPOptions is null)
                throw new ApplicationException("RDMPOptions null");

            if (CatalogueRepository is ITableRepository crtr)
                globals.RDMPOptions.CatalogueConnectionString = crtr.DiscoveredServer.Builder.ConnectionString;
            if (DataExportRepository is ITableRepository dertr)
                globals.RDMPOptions.DataExportConnectionString = dertr.DiscoveredServer.Builder.ConnectionString;

            using (new MicroserviceTester(globals.RabbitOptions ?? throw new InvalidOperationException(), globals.DicomRelationalMapperOptions!))
            {
                using var host = new DicomRelationalMapperHost(globals);
                host.Start();

                Assert.AreEqual(expectedType, host.Consumer?.DatabaseNamer.GetType());
                Assert.IsNotNull(host);

                host.Stop("Test finished");
            }
        }
    }

    public class GuidDatabaseNamerTest
    {
        [Test]
        public void GetExampleName()
        {
            //t6ff062af5538473f801ced2b751c7897test_RAW
            //t6ff062af5538473f801ced2b751c7897DLE_STAGING
            var namer = new GuidDatabaseNamer("test", new Guid("6ff062af-5538-473f-801c-ed2b751c7897"));

            var raw = namer.GetDatabaseName("test", LoadBubble.Raw);
            Console.WriteLine(raw);

            StringAssert.Contains("6ff", raw);

            var staging = namer.GetDatabaseName("test", LoadBubble.Staging);
            Console.WriteLine(staging);

            StringAssert.Contains("6ff", staging);
        }

    }
}
