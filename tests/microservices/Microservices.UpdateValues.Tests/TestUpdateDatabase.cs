using FAnsi;
using FAnsi.Discovery;
using Microservices.UpdateValues.Execution;
using NUnit.Framework;
using Smi.Common.Messages.Updating;
using System;
using System.Data;
using System.Linq;
using Tests.Common;

namespace Microservices.UpdateValues.Tests
{
    public class TestUpdateDatabase : DatabaseTests
    {
        protected DiscoveredTable SetupTestTable(DatabaseType dbType)
        {            
            var type = GetCleanedServer(dbType);

            DataTable dt = new();
            dt.Columns.Add("PatientID");
            dt.Columns.Add("Age");
            dt.Columns.Add("Address");
            
            dt.Rows.Add("123","1","31 Homeland avenue");
            dt.Rows.Add("456","2","32 Homeland avenue");
            dt.Rows.Add("111","3","33 Homeland avenue");
            dt.Rows.Add("111","4","34 Homeland avenue");

            var tblToUpdate = type.CreateTable("MyTableForUpdating",dt);

            Import(tblToUpdate);
            
            return tblToUpdate;
        }

        [TestCase(DatabaseType.MicrosoftSQLServer)]
        [TestCase(DatabaseType.MySql)]
        public void TestUpdateValues_OneTable(DatabaseType dbType)
        {
            var tblToUpdate = SetupTestTable(dbType);

            var updater = new Updater(CatalogueRepository);
            
            //update PatientID that does not exist
            Assert.AreEqual(0,updater.HandleUpdate(new UpdateValuesMessage
            { 
                WhereFields = new[]{ "PatientID"},
                HaveValues = new[]{ "5345"},
                WriteIntoFields = new[]{ "PatientID"},
                Values = new[]{ "999"}
            }), "Should not have been any updates because there is no patient number 5345");
                        
            //update PatientID that DOES exist
            Assert.AreEqual(2,updater.HandleUpdate(new UpdateValuesMessage
            { 
                WhereFields = new[]{ "PatientID"},
                HaveValues = new[]{ "111"},
                WriteIntoFields = new[]{ "PatientID"},
                Values = new[]{ "222"}
            }), "Should have been 2 rows updated");

            Assert.AreEqual(2,tblToUpdate.GetDataTable().Rows.Cast<DataRow>().Count(r=>(int)r["PatientID"] == 222));
        }

        [TestCase(DatabaseType.MicrosoftSQLServer)]
        [TestCase(DatabaseType.MySql)]
        public void TestUpdateValues_OneTable_TwoWHERELogics(DatabaseType dbType)
        {
            var tblToUpdate = SetupTestTable(dbType);

            var updater = new Updater(CatalogueRepository);
            
            //update PatientID that DOES exist, there are 2 patient 111s but only one has the Age 3
            Assert.AreEqual(1,updater.HandleUpdate(new UpdateValuesMessage
            { 
                WhereFields = new[]{ "PatientID","Age"},
                HaveValues = new[]{ "111","3"},
                WriteIntoFields = new[]{ "PatientID"},
                Values = new[]{ "222"}
            }));

            Assert.AreEqual(1,tblToUpdate.GetDataTable().Rows.Cast<DataRow>().Count(r=>(int)r["PatientID"] == 222));
        }
        
        [TestCase(DatabaseType.MicrosoftSQLServer)]
        [TestCase(DatabaseType.MySql)]
        public void TestUpdateValues_OneTable_OperatorGreaterThan(DatabaseType dbType)
        {
            var tblToUpdate = SetupTestTable(dbType);

            var updater = new Updater(CatalogueRepository);
            
            //update PatientID that DOES exist, there are 2 patient 111s both are under 6
            Assert.AreEqual(2,updater.HandleUpdate(new UpdateValuesMessage
            { 
                WhereFields = new[]{ "PatientID","Age"},
                HaveValues = new[]{ "111","6"},
                Operators = new[]{"=","<=" },
                WriteIntoFields = new[]{ "PatientID"},
                Values = new[]{ "222"},
                
            }));

            Assert.AreEqual(2,tblToUpdate.GetDataTable().Rows.Cast<DataRow>().Count(r=>(int)r["PatientID"] == 222));
        }
        [Test]
        public void Test_TableInfoNotFound()
        {
            var updater = new Updater(CatalogueRepository);
            
            var ex = Assert.Throws<Exception>(()=>
            updater.HandleUpdate(new UpdateValuesMessage
            { 
                WhereFields = new[]{ "PatientID","Age"},
                HaveValues = new[]{ "111","3"},
                WriteIntoFields = new[]{ "PatientID"},
                Values = new[]{ "222"},
                ExplicitTableInfo = new int[]{ 999999999}
            }));

            Assert.AreEqual("Could not find all TableInfos IDs=999999999.  Found 0:",ex!.Message);
        }
        
        [Test]
        public void Test_WhereField_NotFound()
        {
            SetupTestTable(DatabaseType.MicrosoftSQLServer);

            var updater = new Updater(CatalogueRepository);
            
            var ex = Assert.Throws<Exception>(()=>
            updater.HandleUpdate(new UpdateValuesMessage
            { 
                WhereFields = new[]{ "Blarg"},
                HaveValues = new[]{ "111"},
                WriteIntoFields = new[]{ "PatientID"},
                Values = new[]{ "222"}
            }));

            TestContext.WriteLine(ex!.Message);

            Assert.AreEqual("Could not find any tables to update that matched the field set UpdateValuesMessage: WhereFields=Blarg WriteIntoFields=PatientID",ex.Message);
        }
        
        [Test]
        public void Test_WriteIntoFields_NotFound()
        {
            SetupTestTable(DatabaseType.MicrosoftSQLServer);

            var updater = new Updater(CatalogueRepository);
            
            var ex = Assert.Throws<Exception>(()=>
            updater.HandleUpdate(new UpdateValuesMessage
            { 
                WhereFields = new[]{ "PatientID"},
                HaveValues = new[]{ "111"},
                WriteIntoFields = new[]{ "Blarg"},
                Values = new[]{ "222"}
            }));

            TestContext.WriteLine(ex!.Message);

            Assert.AreEqual("Could not find any tables to update that matched the field set UpdateValuesMessage: WhereFields=PatientID WriteIntoFields=Blarg",ex.Message);
        }


    }
}
