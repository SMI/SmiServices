using FAnsi;
using FAnsi.Discovery;
using FAnsi.Extensions;
using NUnit.Framework;
using Rdmp.Core.DataLoad.Triggers;
using Rdmp.Core.DataLoad.Triggers.Implementations;
using Rdmp.Core.MapsDirectlyToDatabaseTable;
using Rdmp.Core.ReusableLibraryCode.Checks;
using SmiServices.Applications.TriggerUpdates;
using SmiServices.Common.Options;
using SmiServices.IntegrationTests;
using SmiServices.Microservices.IdentifierMapper.Swappers;
using SmiServices.Microservices.UpdateValues;
using SmiServices.UnitTests.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Tests.Common;


namespace SmiServices.UnitTests.Applications.TriggerUpdates
{
    [RequiresRabbit]
    class MapperSourceIntegrationTest : DatabaseTests
    {

        [TestCase(DatabaseType.MicrosoftSQLServer)]
        [TestCase(DatabaseType.MySql)]
        public void MapperSource_IntegrationTest(DatabaseType dbType)
        {
            var db = GetCleanedServer(dbType);

            DataTable dt = new();
            dt.Columns.Add("PatientID", typeof(string));
            dt.Columns.Add("StudyDescription", typeof(string));
            dt.SetDoNotReType(true);

            // We have a live table with anonymised data.  There is one person with a known ECHI 0101010101=0A0A0A0A0A
            dt.Rows.Add("0A0A0A0A0A", "CT Head");

            //There are 2 people for whome we have added temporary identifiers
            dt.Rows.Add("bbb-bbb-bbb", "CT Tail");
            dt.Rows.Add("ccc-ccc-ccc", "CT Wings");

            var liveTable = db.CreateTable("MyLiveTable", dt);

            DiscoveredTable map;

            using (var dtMap = new DataTable())
            {
                dtMap.Columns.Add("CHI");
                dtMap.Columns.Add("ECHI");

                dtMap.PrimaryKey = new[] { dtMap.Columns["CHI"]! };

                dtMap.Rows.Add("0101010101", "0A0A0A0A0A");
                map = db.CreateTable("Map", dtMap);
            }

            // Import into RDMP the live table so we have a TableInfo pointer to it floating around
            Import(liveTable);

            var mapperOptions = new IdentifierMapperOptions
            {
                MappingTableName = map.GetFullyQualifiedName(),
                MappingConnectionString = db.Server.Builder.ConnectionString,
                SwapColumnName = "CHI",
                ReplacementColumnName = "ECHI",
                MappingDatabaseType = db.Server.DatabaseType,
                SwapperType = typeof(TableLookupWithGuidFallbackSwapper).FullName
            };

            var swapper = new TableLookupWithGuidFallbackSwapper();
            swapper.Setup(mapperOptions);

            var guidTable = swapper.GetGuidTableIfAny(mapperOptions);
            Assert.Multiple(() =>
            {
                Assert.That(guidTable, Is.Not.Null);
                Assert.That(guidTable?.GetRowCount(), Is.EqualTo(0), "No temporary guids should exist yet");
                Assert.That(map.GetRowCount(), Is.EqualTo(1), "We should have a mapping table with 1 entry");
            });

            guidTable.Insert(new Dictionary<string, object>
            {
                { "CHI","0202020202" },
                { TableLookupWithGuidFallbackSwapper.GuidColumnName,"bbb-bbb-bbb"}
                });
            guidTable.Insert(new Dictionary<string, object>
            {
                { "CHI","0303030303" },
                { TableLookupWithGuidFallbackSwapper.GuidColumnName,"ccc-ccc-ccc"}
                });

            Assert.Multiple(() =>
            {
                Assert.That(map.GetRowCount(), Is.EqualTo(1), "We should have a mapping table with 1 entry");
                Assert.That(guidTable.GetRowCount(), Is.EqualTo(2), "We should have a temporary guid for 0202020202");
            });

            // make a fake data load into this table (create trigger and insert/update)
            var triggerImplementer = new TriggerImplementerFactory(dbType).Create(map);
            triggerImplementer.CreateTrigger(ThrowImmediatelyCheckNotifier.Quiet);

            //create a brand new mapping 
            map.Insert(new Dictionary<string, object>
            {
                {"CHI","0303030303" },
                {"ECHI","0C0C0C0C0C" },
                {SpecialFieldNames.ValidFrom,DateTime.Now },
                {SpecialFieldNames.DataLoadRunID,55},
                });

            var globals = new GlobalOptionsFactory().Load(nameof(MapperSource_IntegrationTest));

            var cliOptions = new TriggerUpdatesFromMapperOptions
            {
                DateOfLastUpdate = new DateTime(2020, 01, 01),
                LiveDatabaseFieldName = "PatientID",
                Qualifier = '\'',

            };

            globals.UseTestValues(
                RequiresRabbit.GetConnectionFactory(),
                RequiresMongoDb.GetMongoClientSettings(),
                RequiresRelationalDb.GetRelationalDatabaseConnectionStrings(),
                ((TableRepository)RepositoryLocator.CatalogueRepository).ConnectionStringBuilder,
                ((TableRepository)RepositoryLocator.DataExportRepository).ConnectionStringBuilder);


            //make sure the identifier mapper goes to the right table
            globals.IdentifierMapperOptions!.MappingConnectionString = db.Server.Builder.ConnectionString;
            globals.IdentifierMapperOptions.MappingDatabaseType = dbType;
            globals.IdentifierMapperOptions.MappingTableName = map.GetFullyQualifiedName();
            globals.IdentifierMapperOptions.SwapperType = typeof(TableLookupWithGuidFallbackSwapper).FullName;

            using (var tester = new MicroserviceTester(globals.RabbitOptions!, globals.CohortExtractorOptions!))
            {
                tester.CreateExchange(globals.TriggerUpdatesOptions!.ExchangeName!, globals.UpdateValuesOptions!.QueueName);

                var sourceHost = new TriggerUpdatesHost(globals, new MapperSource(globals, cliOptions));
                var destHost = new UpdateValuesHost(globals);

                sourceHost.Start();
                tester.StopOnDispose.Add(sourceHost);

                destHost.Start();
                tester.StopOnDispose.Add(destHost);


                //wait till updater is done updating the live table
                TestTimelineAwaiter.Await(() => destHost.Consumer!.AckCount == 1);
            }

            var liveDtAfter = liveTable.GetDataTable();

            Assert.Multiple(() =>
            {
                Assert.That(liveDtAfter.Rows.Cast<DataRow>().Count(r => (string)r["PatientID"] == "0A0A0A0A0A"), Is.EqualTo(1), "Expected original data to still be intact");
                Assert.That(liveDtAfter.Rows.Cast<DataRow>().Count(r => (string)r["PatientID"] == "bbb-bbb-bbb"), Is.EqualTo(1), "Expected unknown CHI with guid bbb to still be unknown");
                Assert.That(liveDtAfter.Rows.Cast<DataRow>().Count(r => (string)r["PatientID"] == "0C0C0C0C0C"), Is.EqualTo(1), "Expected the unknown CHI ccc to be now known as 0C0C0C0C0C");
            });
        }
    }
}
