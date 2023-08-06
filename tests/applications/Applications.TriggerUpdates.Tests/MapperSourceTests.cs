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
using Applications.TriggerUpdates.Execution;
using Applications.TriggerUpdates.Options;
using Tests.Common;


namespace Applications.TriggerUpdates.Tests
{

    class MapperSourceTests : DatabaseTests
    {
        /// <summary>
        /// Sets up a CHI/ECHI mapping table with fallback guid and populates each table with a single record.
        /// 0101010101 is a known CHI and 0202020202 is an known one (which was assigned a temporary guid mapping).
        /// Also prepares the main map table for DLE loading (<see cref="TriggerImplementer"/>)
        /// </summary>
        /// <param name="dbType"></param>
        /// <param name="map"></param>
        /// <param name="guidTable"></param>
        /// <param name="mapperOptions"></param>
        /// <param name="guids">true to create a <see cref="TableLookupWithGuidFallbackSwapper"/> otherwise creates a  <see cref="TableLookupSwapper"/></param>
        private void SetupMappers(DatabaseType dbType, out DiscoveredTable map, out DiscoveredTable? guidTable, out IdentifierMapperOptions mapperOptions, bool guids=true)
        {
            var db = GetCleanedServer(dbType);

            using (var dt = new DataTable())
            {
                dt.Columns.Add("CHI");
                dt.Columns.Add("ECHI");

                dt.PrimaryKey = new []{ dt.Columns["CHI"]!};

                dt.Rows.Add("0101010101", "0A0A0A0A0A");
                map = db.CreateTable("Map",dt);
            }

            mapperOptions = new IdentifierMapperOptions
            {
                MappingTableName = map.GetFullyQualifiedName(),
                MappingConnectionString = db.Server.Builder.ConnectionString,
                SwapColumnName = "CHI",
                ReplacementColumnName = "ECHI",
                MappingDatabaseType = db.Server.DatabaseType,
                SwapperType = (guids ? typeof(TableLookupWithGuidFallbackSwapper):typeof(TableLookupSwapper)).FullName
            };

            if(guids)
            {
                var swapper = new TableLookupWithGuidFallbackSwapper();
                swapper.Setup(mapperOptions);

                guidTable = swapper.GetGuidTableIfAny(mapperOptions);
                Assert.AreEqual(0,guidTable?.GetRowCount(), "No temporary guids should exist yet");
                Assert.AreEqual(1,map.GetRowCount(),"We should have a mapping table with 1 entry");
            
                //lookup an as yet unknown value
                swapper.GetSubstitutionFor("0202020202",out _);

                Assert.AreEqual(1,map.GetRowCount(),"We should have a mapping table with 1 entry");
                Assert.AreEqual(1,guidTable?.GetRowCount(), "We should have a temporary guid for 0202020202");
            }
            else
                guidTable = null;

            // make a fake data load into this table (create trigger and insert/update)
            var triggerImplementer = new TriggerImplementerFactory(dbType).Create(map);
            triggerImplementer.CreateTrigger(new ThrowImmediatelyCheckNotifier());
        }

        [TestCase(DatabaseType.MySql)]
        [TestCase(DatabaseType.MicrosoftSQLServer)]
        public void TestMapperSource_BrandNewMapping(DatabaseType dbType)
        {
            SetupMappers(dbType, out DiscoveredTable map, out DiscoveredTable? _, out IdentifierMapperOptions mapperOptions);

            //create a brand new mapping 
            map.Insert(new Dictionary<string, object>
            { 
                {"CHI","0303030303" },
                {"ECHI","0C0C0C0C0C" },
                {SpecialFieldNames.ValidFrom,DateTime.Now },
                {SpecialFieldNames.DataLoadRunID,55},
                });

            var source = new MapperSource(new GlobalOptions {IdentifierMapperOptions = mapperOptions,TriggerUpdatesOptions = new TriggerUpdatesOptions()  }, new TriggerUpdatesFromMapperOptions {DateOfLastUpdate = new DateTime(2020,01,01)});

            Assert.IsEmpty(source.GetUpdates(), "Since 0303030303 has never before been seen (not in guid table) we don't have any existing mappings to update");
        }
        
        [TestCase(DatabaseType.MySql)]
        [TestCase(DatabaseType.MicrosoftSQLServer)]
        public void TestMapperSource_NoArchiveTable(DatabaseType dbType)
        {
            SetupMappers(dbType, out DiscoveredTable map, out DiscoveredTable? guidTable, out IdentifierMapperOptions mapperOptions);
            
            var archive = map.Database.ExpectTable(map.GetRuntimeName() + "_Archive");
            archive.Drop();

            var source = new MapperSource(new GlobalOptions {IdentifierMapperOptions = mapperOptions,TriggerUpdatesOptions = new TriggerUpdatesOptions()  }, new TriggerUpdatesFromMapperOptions {DateOfLastUpdate = new DateTime(2020,01,01)});
            var ex = Assert.Throws<Exception>(()=>source.GetUpdates().ToArray());

            StringAssert.StartsWith("No Archive table exists for mapping table",ex!.Message);
        }
        
        [TestCase(DatabaseType.MySql)]
        [TestCase(DatabaseType.MicrosoftSQLServer)]
        public void TestMapperSource_UpdatedMapping(DatabaseType dbType)
        {
            SetupMappers(dbType, out DiscoveredTable map, out DiscoveredTable? _, out IdentifierMapperOptions mapperOptions);

            // Simulate a data load that changes the mapping of CHI 0101010101 from 0A0A0A0A0A to 0Z0Z0Z0Z0Z
            using(var con = map.Database.Server.GetConnection())
            {
                con.Open();
                Assert.AreEqual(1,map.GetCommand($"UPDATE {map.GetFullyQualifiedName()} SET ECHI = '0Z0Z0Z0Z0Z' WHERE CHI = '0101010101'",con).ExecuteNonQuery());

            }
            
            var archive = map.Database.ExpectTable(map.GetRuntimeName() + "_Archive");
            Assert.IsTrue(archive.Exists(),"Archive table should definitely be there, we created the trigger after all");
            Assert.AreEqual(1,archive.GetRowCount(), "Expected the old ECHI to have an entry in the archive when it was updated");
                        
            Assert.AreEqual(1,map.GetRowCount(),"We should have a mapping table with 1 entry");

            var source = new MapperSource(new GlobalOptions {IdentifierMapperOptions = mapperOptions,TriggerUpdatesOptions = new TriggerUpdatesOptions()  }, new TriggerUpdatesFromMapperOptions {DateOfLastUpdate = new DateTime(2020,01,01)});

            var msg = source.GetUpdates().ToArray();
            Assert.IsNotNull(msg);

            Assert.AreEqual("ECHI",msg[0].WhereFields.Single());
            Assert.AreEqual("ECHI",msg[0].WriteIntoFields.Single());

            Assert.AreEqual("0A0A0A0A0A",msg[0].HaveValues.Single());
            Assert.AreEqual("0Z0Z0Z0Z0Z",msg[0].Values.Single());

            Assert.AreEqual(1,msg.Length, "We expected only one update");
        }

        [TestCase(DatabaseType.MySql)]
        [TestCase(DatabaseType.MicrosoftSQLServer)]
        public void TestMapperSource_UpdatedMapping_WithExplicitDifferentColumnName(DatabaseType dbType)
        {
            SetupMappers(dbType, out DiscoveredTable map, out DiscoveredTable? _, out IdentifierMapperOptions mapperOptions);

            // Simulate a data load that changes the mapping of CHI 0101010101 from 0A0A0A0A0A to 0Z0Z0Z0Z0Z
            using(var con = map.Database.Server.GetConnection())
            {
                con.Open();
                Assert.AreEqual(1,map.GetCommand($"UPDATE {map.GetFullyQualifiedName()} SET ECHI = '0Z0Z0Z0Z0Z' WHERE CHI = '0101010101'",con).ExecuteNonQuery());
            }
            
            var archive = map.Database.ExpectTable(map.GetRuntimeName() + "_Archive");
            Assert.IsTrue(archive.Exists(),"Archive table should definitely be there, we created the trigger after all");
            Assert.AreEqual(1,archive.GetRowCount(), "Expected the old ECHI to have an entry in the archive when it was updated");
                        
            Assert.AreEqual(1,map.GetRowCount(),"We should have a mapping table with 1 entry");

            var source = new MapperSource(new GlobalOptions {IdentifierMapperOptions = mapperOptions,TriggerUpdatesOptions = new TriggerUpdatesOptions()  }, new TriggerUpdatesFromMapperOptions
            {
                DateOfLastUpdate = new DateTime(2020,01,01),
                
                // This is the thing we are testing
                LiveDatabaseFieldName = "PatientID"
            });

            var msg = source.GetUpdates().ToArray();
            Assert.IsNotNull(msg);

            Assert.AreEqual("PatientID",msg[0].WhereFields.Single(),"Expected the column in the live database to be updated to be the explicit column name we provided on the command line");
            Assert.AreEqual("PatientID",msg[0].WriteIntoFields.Single(),"Expected the column in the live database to be updated to be the explicit column name we provided on the command line");

            Assert.AreEqual("0A0A0A0A0A",msg[0].HaveValues.Single());
            Assert.AreEqual("0Z0Z0Z0Z0Z",msg[0].Values.Single());

            Assert.AreEqual(1,msg.Length, "We expected only one update");
        }

        
        [TestCase(DatabaseType.MySql)]
        [TestCase(DatabaseType.MicrosoftSQLServer)]
        public void TestMapperSource_UpdatedMapping_Qualifier(DatabaseType dbType)
        {
            SetupMappers(dbType, out DiscoveredTable map, out DiscoveredTable? _, out IdentifierMapperOptions mapperOptions);

            // Simulate a data load that changes the mapping of CHI 0101010101 from 0A0A0A0A0A to 0Z0Z0Z0Z0Z
            using(var con = map.Database.Server.GetConnection())
            {
                con.Open();
                Assert.AreEqual(1,map.GetCommand($"UPDATE {map.GetFullyQualifiedName()} SET ECHI = null WHERE CHI = '0101010101'",con).ExecuteNonQuery());
            }
            
            var archive = map.Database.ExpectTable(map.GetRuntimeName() + "_Archive");
            Assert.IsTrue(archive.Exists(),"Archive table should definitely be there, we created the trigger after all");
            Assert.AreEqual(1,archive.GetRowCount(), "Expected the old ECHI to have an entry in the archive when it was updated");
                        
            Assert.AreEqual(1,map.GetRowCount(),"We should have a mapping table with 1 entry");

            var source = new MapperSource(new GlobalOptions {IdentifierMapperOptions = mapperOptions,TriggerUpdatesOptions = new TriggerUpdatesOptions()  }, new TriggerUpdatesFromMapperOptions
            {
                DateOfLastUpdate = new DateTime(2020,01,01),
                
                // This is the thing we are testing
                Qualifier = '\'',
                LiveDatabaseFieldName = "PatientID"
            });

            var msg = source.GetUpdates().ToArray();
            Assert.IsNotNull(msg);

            Assert.AreEqual("PatientID",msg[0].WhereFields.Single(),"Expected the column in the live database to be updated to be the explicit column name we provided on the command line");
            Assert.AreEqual("PatientID",msg[0].WriteIntoFields.Single(),"Expected the column in the live database to be updated to be the explicit column name we provided on the command line");

            Assert.AreEqual("'0A0A0A0A0A'",msg[0].HaveValues.Single());
            Assert.AreEqual("null",msg[0].Values.Single());

            Assert.AreEqual(1,msg.Length, "We expected only one update");
        }

        
        
        [TestCase(DatabaseType.MySql)]
        [TestCase(DatabaseType.MicrosoftSQLServer)]
        public void TestMapperSource_GuidMappingNowExists(DatabaseType dbType)
        {
            SetupMappers(dbType, out DiscoveredTable map, out DiscoveredTable? guidTable, out IdentifierMapperOptions mapperOptions);

            // Simulate a data load that inserts the previously unknown value 0202020202 into the mapping as 0X0X0X0X0X
            // The value 0202020202 is in the guid mapping table! so we would expect a global system update to be issued for the temporary guid mapping to the new legit mapping
            map.Insert(new Dictionary<string, object>
            { 
                {"CHI","0202020202" },
                {"ECHI","0X0X0X0X0X" },
                {SpecialFieldNames.ValidFrom,DateTime.Now },
                {SpecialFieldNames.DataLoadRunID,55},
                });
            
            var oldTempGuid = guidTable!.GetDataTable().Rows[0][TableLookupWithGuidFallbackSwapper.GuidColumnName];
            Assert.IsNotNull(oldTempGuid);
 
            Assert.AreEqual(2,map.GetRowCount(),"We should have a mapping table with 2 entries, the old existing one 0101010101 and a new one 0202020202");

            var source = new MapperSource(new GlobalOptions {IdentifierMapperOptions = mapperOptions,TriggerUpdatesOptions = new TriggerUpdatesOptions()  }, new TriggerUpdatesFromMapperOptions {DateOfLastUpdate = new DateTime(2020,01,01)});

            var msg = source.GetUpdates().ToArray();
            Assert.IsNotNull(msg);

            Assert.AreEqual("ECHI",msg[0].WhereFields.Single());
            Assert.AreEqual("ECHI",msg[0].WriteIntoFields.Single());

            Assert.AreEqual(oldTempGuid,msg[0].HaveValues.Single(),"Expected the temporary guid to be the thing we are searching for to replace");
            Assert.AreEqual("0X0X0X0X0X",msg[0].Values.Single(),"Expected the replacement value to be the new legit mapping");

            Assert.AreEqual(1,msg.Length, "We expected only one update");
        }
        
        [TestCase(DatabaseType.MySql)]
        [TestCase(DatabaseType.MicrosoftSQLServer)]
        public void Test_MapperSource_NoGuids(DatabaseType dbType)
        {
            SetupMappers(dbType, out DiscoveredTable map, out DiscoveredTable? _, out IdentifierMapperOptions mapperOptions,false);

            // Simulate a data load that changes the mapping of CHI 0101010101 from 0A0A0A0A0A to 0Z0Z0Z0Z0Z
            using(var con = map.Database.Server.GetConnection())
            {
                con.Open();
                Assert.AreEqual(1,map.GetCommand($"UPDATE {map.GetFullyQualifiedName()} SET ECHI = '0Z0Z0Z0Z0Z' WHERE CHI = '0101010101'",con).ExecuteNonQuery());
            }
            
            var archive = map.Database.ExpectTable(map.GetRuntimeName() + "_Archive");
            Assert.IsTrue(archive.Exists(),"Archive table should definitely be there, we created the trigger after all");
            Assert.AreEqual(1,archive.GetRowCount(), "Expected the old ECHI to have an entry in the archive when it was updated");
                        
            Assert.AreEqual(1,map.GetRowCount(),"We should have a mapping table with 1 entry");

            var source = new MapperSource(new GlobalOptions {IdentifierMapperOptions = mapperOptions,TriggerUpdatesOptions = new TriggerUpdatesOptions() }, new TriggerUpdatesFromMapperOptions {DateOfLastUpdate = new DateTime(2020,01,01)});

            var msg = source.GetUpdates().ToArray();
            Assert.IsNotNull(msg);

            Assert.AreEqual("ECHI",msg[0].WhereFields.Single());
            Assert.AreEqual("ECHI",msg[0].WriteIntoFields.Single());

            Assert.AreEqual("0A0A0A0A0A",msg[0].HaveValues.Single());
            Assert.AreEqual("0Z0Z0Z0Z0Z",msg[0].Values.Single());

            Assert.AreEqual(1,msg.Length, "We expected only one update");
        }
    }
}
