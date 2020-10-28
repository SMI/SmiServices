using FAnsi;
using Microservices.UpdateValues.Execution;
using NUnit.Framework;
using Smi.Common.Messages.Updating;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Tests.Common;

namespace Microservices.UpdateValues.Tests
{
    public class TestUpdateDatabase : DatabaseTests
    {
        [TestCase(DatabaseType.MicrosoftSQLServer)]
        [TestCase(DatabaseType.MySql)]
        public void TestUpdateValues_OneTable(DatabaseType dbType)
        {
            var type = GetCleanedServer(dbType);

            DataTable dt = new DataTable();
            dt.Columns.Add("PatientID");
            dt.Columns.Add("Age");
            dt.Columns.Add("Address");
            
            dt.Rows.Add("123","1","31 Homeland avenue");
            dt.Rows.Add("456","2","32 Homeland avenue");
            dt.Rows.Add("111","3","33 Homeland avenue");
            dt.Rows.Add("111","4","34 Homeland avenue");

            var tblToUpdate = type.CreateTable("MyTableForUpdating",dt);

            Import(tblToUpdate);

            var updater = new Updater(CatalogueRepository);
            
            Assert.AreEqual(0,updater.HandleUpdate(new UpdateValueMessage()
            { 
                WhereFields = new[]{ "PatientID"},
                HaveValues = new[]{ "5345"},
                WriteIntoFields = new[]{ "PatientID"},
                Values = new[]{ "999"}
            }), "Should not have been any updates because there is no patient number 5345");
                        
            Assert.AreEqual(2,updater.HandleUpdate(new UpdateValueMessage()
            { 
                WhereFields = new[]{ "PatientID"},
                HaveValues = new[]{ "111"},
                WriteIntoFields = new[]{ "PatientID"},
                Values = new[]{ "222"}
            }), "Should have been 2 rows updated");

            Assert.AreEqual(2,tblToUpdate.GetDataTable().Rows.Cast<DataRow>().Count(r=>(int)r["PatientID"] == 222));
        }
    }
}
