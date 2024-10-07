using FAnsi;
using NUnit.Framework;
using Rdmp.Core.Curation.Data.DataLoad;
using Rdmp.Core.MapsDirectlyToDatabaseTable;
using SmiServices.Common.Options;
using SmiServices.Microservices.DicomRelationalMapper;
using SmiServices.Microservices.DicomRelationalMapper.Namers;
using SmiServices.UnitTests.Common;
using System;
using System.Data;
using Tests.Common;

namespace SmiServices.IntegrationTests.Microservices.DicomRelationalMapper
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
            lmd.LinkToCatalogue(cata);
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

                Assert.Multiple(() =>
                {
                    Assert.That(host.Consumer?.DatabaseNamer.GetType(), Is.EqualTo(expectedType));
                    Assert.That(host, Is.Not.Null);
                });

                host.Stop("Test finished");
            }
        }
    }
}
