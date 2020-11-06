using FAnsi;
using FAnsi.Discovery;
using Microservices.IdentifierMapper.Execution.Swappers;
using NUnit.Framework;
using Rdmp.Core.DataLoad.Triggers;
using Rdmp.Core.DataLoad.Triggers.Implementations;
using ReusableLibraryCode.Checks;
using Smi.Common.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Tests.Common;
using TriggerUpdates;
using TriggerUpdates.Execution;

namespace Applications.TriggerUpdates.Tests
{

    class MapperSourceTests : DatabaseTests
    {
        /// <summary>
        /// Sets up a CHI/ECHI mapping table with fallback guid and populates each table with a single record.
        /// 0101010101 is a known CHI and 0202020202 is an known one (which was assigned a temporary guid mapping).
        /// Also prepares the main map table for DLE loading (<see cref="TriggerImplementer")/>)
        /// </summary>
        /// <param name="dbType"></param>
        /// <param name="map"></param>
        /// <param name="guidTable"></param>
        private void SetupMappers(DatabaseType dbType, out DiscoveredTable map, out DiscoveredTable guidTable, out IdentifierMapperOptions mapperOptions)
        {
            var db = GetCleanedServer(dbType);

            using (var dt = new DataTable())
            {
                dt.Columns.Add("CHI");
                dt.Columns.Add("ECHI");

                dt.PrimaryKey = new []{ dt.Columns["CHI"]};

                dt.Rows.Add("0101010101", "0A0A0A0A0A");
                map = db.CreateTable("Map",dt);
            }

            mapperOptions = new IdentifierMapperOptions()
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

            guidTable = swapper.GetGuidTable(mapperOptions);

            Assert.AreEqual(1,map.GetRowCount(),"We should have a mapping table with 1 entry");
            Assert.AreEqual(0,guidTable.GetRowCount(), "No temporary guids should exist yet");

            //lookup an as yet unknown value
            swapper.GetSubstitutionFor("0202020202",out _);

            Assert.AreEqual(1,map.GetRowCount(),"We should have a mapping table with 1 entry");
            Assert.AreEqual(1,guidTable.GetRowCount(), "We should have a temporary guid for 0202020202");

            // make a fake data load into this table (create trigger and insert/update)
            var triggerImplementer = new TriggerImplementerFactory(dbType).Create(map);
            triggerImplementer.CreateTrigger(new ThrowImmediatelyCheckNotifier());
        }

        [TestCase(DatabaseType.MySql)]
        [TestCase(DatabaseType.MicrosoftSQLServer)]
        public void TestMapperSource_BrandNewMapping(DatabaseType dbType)
        {
            SetupMappers(dbType, out DiscoveredTable map, out DiscoveredTable guidTable, out IdentifierMapperOptions mapperOptions);

            //create a brand new mapping 
            map.Insert(new Dictionary<string, object>(){ 
                {"CHI","0303030303" },
                {"ECHI","0C0C0C0C0C" },
                {SpecialFieldNames.ValidFrom,DateTime.Now },
                {SpecialFieldNames.DataLoadRunID,55},
                });

            var source = new MapperSource(new GlobalOptions(){IdentifierMapperOptions = mapperOptions }, new TriggerUpdatesFromMapperOptions());

            Assert.IsNull(source.Next(), "Since 0303030303 has never before been seen (not in guid table) we don't have any existing mappings to update");
        }

        
        [TestCase(DatabaseType.MySql)]
        [TestCase(DatabaseType.MicrosoftSQLServer)]
        public void TestMapperSource_UpdatedMapping(DatabaseType dbType)
        {
            SetupMappers(dbType, out DiscoveredTable map, out DiscoveredTable guidTable, out IdentifierMapperOptions mapperOptions);

            // Simulate a data load that changes the mapping of CHI 0101010101 from 0A0A0A0A0A to 0Z0Z0Z0Z0Z
            using(var con = map.Database.Server.GetConnection())
            {
                con.Open();
                Assert.AreEqual(1,map.GetCommand($"UPDATE {map.GetFullyQualifiedName()} SET ECHI = '0Z0Z0Z0Z0Z' WHERE CHI = '0101010101'",con).ExecuteNonQuery());

            }
            
            var archive = map.Database.ExpectTable(map.GetRuntimeName() + "_Archive");
            Assert.IsTrue(archive.Exists(),"Archive table should definetly be there, we created the trigger after all");
            Assert.AreEqual(1,archive.GetRowCount(), "Expected the old ECHI to have an entry in the archive when it was updated");
                        
            Assert.AreEqual(1,map.GetRowCount(),"We should have a mapping table with 1 entry");

            var source = new MapperSource(new GlobalOptions(){IdentifierMapperOptions = mapperOptions }, new TriggerUpdatesFromMapperOptions());

            var msg = source.Next();
            Assert.IsNotNull(msg);

            Assert.AreEqual("ECHI",msg.WhereFields.Single());
            Assert.AreEqual("ECHI",msg.WriteIntoFields.Single());

            Assert.AreEqual("0A0A0A0A0A",msg.HaveValues.Single());
            Assert.AreEqual("0Z0Z0Z0Z0Z",msg.Values.Single());

            Assert.IsNull(source.Next(), "We expected only one update, the next call should have returned null");
        }

    }
}
