﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;
using BadMedicine;
using BadMedicine.Dicom;
using Dicom;
using FAnsi.Discovery;
using Microservices.CohortExtractor.Audit;
using Microservices.CohortExtractor.Execution;
using Microservices.CohortExtractor.Execution.RequestFulfillers;
using Microservices.CohortExtractor.Execution.RequestFulfillers.Dynamic;
using NUnit.Framework;
using Rdmp.Core.DataLoad.Triggers;
using Smi.Common.Messages.Extraction;
using Smi.Common.Options;
using Smi.Common.Tests;
using Tests.Common;
using TypeGuesser;
using DatabaseType = FAnsi.DatabaseType;

namespace Microservices.CohortExtractor.Tests
{
    class ExtractionSuperTableTests : DatabaseTests
    {

        private DiscoveredTable BuildExampleExtractionTable(DiscoveredDatabase db,string modality, int recordCount, bool useDcmFileExtension)
        {
            var tbl = db.CreateTable(modality + "_IsExtractable",
                new[]
                {
                    new DatabaseColumnRequest("StudyInstanceUID", new DatabaseTypeRequest(typeof(string), 64), false),
                    new DatabaseColumnRequest("SeriesInstanceUID", new DatabaseTypeRequest(typeof(string), 64), false),
                    new DatabaseColumnRequest("SOPInstanceUID", new DatabaseTypeRequest(typeof(string), 64), false){IsPrimaryKey = true},
                    new DatabaseColumnRequest("IsExtractableToDisk", new DatabaseTypeRequest(typeof(bool))),
                    new DatabaseColumnRequest("IsExtractableToDisk_Reason", new DatabaseTypeRequest(typeof(string), 512)),
                    new DatabaseColumnRequest("RelativeFileArchiveURI", new DatabaseTypeRequest(typeof(string), 512), false),
                    new DatabaseColumnRequest("IsOriginal", new DatabaseTypeRequest(typeof(bool)), false),
                    new DatabaseColumnRequest("IsPrimary", new DatabaseTypeRequest(typeof(bool)), false),
                    new DatabaseColumnRequest(SpecialFieldNames.DataLoadRunID, new DatabaseTypeRequest(typeof(int))),
                    new DatabaseColumnRequest(SpecialFieldNames.ValidFrom, new DatabaseTypeRequest(typeof(DateTime))),
                });

            if (recordCount > 0)
            {
                var r = new Random(500);

                DicomDataGenerator g = new DicomDataGenerator(r,null);
                g.MaximumImages = recordCount;
                
                var persons =  new PersonCollection();
                persons.GeneratePeople(500,r);

                while(recordCount > 0)
                    foreach (var image in g.GenerateStudyImages(persons.People[r.Next(persons.People.Length)],out var study))
                    {
                        tbl.Insert(new Dictionary<string, object>()
                        {
                            {"StudyInstanceUID", image.GetSingleValue<string>(DicomTag.StudyInstanceUID)},
                            {"SeriesInstanceUID", image.GetSingleValue<string>(DicomTag.SeriesInstanceUID)},
                            {"SOPInstanceUID", image.GetSingleValue<string>(DicomTag.SOPInstanceUID)},
                            
                            {"IsExtractableToDisk", true},
                            {"IsExtractableToDisk_Reason", DBNull.Value},
                            {"RelativeFileArchiveURI", image.GetSingleValue<string>(DicomTag.SOPInstanceUID) + (useDcmFileExtension ? ".dcm" :"")},
                            {"IsOriginal", image.GetValues<string>(DicomTag.ImageType)[0] == "ORIGINAL"},
                            {"IsPrimary", image.GetValues<string>(DicomTag.ImageType)[1] == "PRIMARY"},
                            
                            {SpecialFieldNames.DataLoadRunID, 1},
                            {SpecialFieldNames.ValidFrom, DateTime.Now},

                        });
                        
                        recordCount--;
                        
                        if(recordCount <=0)
                            break;
                    }
            }
            
            return tbl;
        }


        [TestCase(DatabaseType.MicrosoftSQLServer,true), RequiresRelationalDb(DatabaseType.MicrosoftSQLServer)]
        [TestCase(DatabaseType.MySql,true), RequiresRelationalDb(DatabaseType.MySql)]
        [TestCase(DatabaseType.MicrosoftSQLServer,false), RequiresRelationalDb(DatabaseType.MicrosoftSQLServer)]
        [TestCase(DatabaseType.MySql,false), RequiresRelationalDb(DatabaseType.MySql)]
        public void Test_OnlyExtractableImages(DatabaseType dbType,bool useDynamic)
        {
            var db = GetCleanedServer(dbType);

            //create table with 100 rows
            var tbl = BuildExampleExtractionTable(db, "CT", 100,true);

            Assert.AreEqual(100,tbl.GetRowCount());

            var cata = Import(tbl);
            
            List<string> studies;

            //fetch all unique studies from the database
            using(var dt = tbl.GetDataTable())
                studies = dt.Rows.Cast<DataRow>().Select(r => r["StudyInstanceUID"]).Cast<string>().Distinct().ToList();

            Assert.GreaterOrEqual(studies.Count,2,"Expected at least 2 studies to be randomly generated in database");

            //Create message to extract all the studies by StudyInstanceUID
            var msgIn = new ExtractionRequestMessage();
            msgIn.KeyTag = DicomTag.StudyInstanceUID.DictionaryEntry.Keyword;
            msgIn.ExtractionIdentifiers = studies;

            int matches = 0;
            
            //The strategy pattern implementation that goes to the database but also considers reason
            var fulfiller = new FromCataloguesExtractionRequestFulfiller(new[] {cata});
            fulfiller.Rejector = useDynamic? (IRejector) new DynamicRejector():new TestRejector();
            
            foreach (ExtractImageCollection msgOut in fulfiller.GetAllMatchingFiles(msgIn, new NullAuditExtractions()))
            {
                matches += msgOut.Accepted.Count();
                Assert.IsEmpty(msgOut.Rejected);
            }

            //currently all images are extractable
            Assert.AreEqual(100,matches);

            //now make 10 not extractable
            using (var con = tbl.Database.Server.GetConnection())
            {
                con.Open();

                string sql = GetUpdateTopXSql(tbl,10, "Set IsExtractableToDisk=0, IsExtractableToDisk_Reason = 'We decided NO!'");

                //make the top 10 not extractable
                using (var cmd = tbl.Database.Server.GetCommand(sql,con))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            matches = 0;
            int rejections = 0;

            foreach (ExtractImageCollection msgOut in fulfiller.GetAllMatchingFiles(msgIn, new NullAuditExtractions()))
            {
                matches += msgOut.Accepted.Count;
                rejections += msgOut.Rejected.Count;

                Assert.IsTrue(msgOut.Rejected.All(v=>v.RejectReason.Equals("We decided NO!")));
            }

            Assert.AreEqual(90,matches);
            Assert.AreEqual(10, rejections);

        }


        [TestCase(DatabaseType.MicrosoftSQLServer), RequiresRelationalDb(DatabaseType.MicrosoftSQLServer)]
        [TestCase(DatabaseType.MySql), RequiresRelationalDb(DatabaseType.MySql)]
        public void Test_OnlyListedModalities(DatabaseType dbType)
        {
            var db = GetCleanedServer(dbType);

            //create table with 100 rows
            var tblCT = BuildExampleExtractionTable(db, "CT", 70,true);
            var tblMR = BuildExampleExtractionTable(db, "MR", 30,true);
            
            var cataCT = Import(tblCT);
            var cataMR = Import(tblMR);
            
            List<string> studies = new List<string>();

            //fetch all unique studies from the database
            using(var dt = tblCT.GetDataTable())
                studies.AddRange(dt.Rows.Cast<DataRow>().Select(r => r["StudyInstanceUID"]).Cast<string>().Distinct());
            using(var dt = tblMR.GetDataTable())
                studies.AddRange(dt.Rows.Cast<DataRow>().Select(r => r["StudyInstanceUID"]).Cast<string>().Distinct());
            
            //Create message to extract all the series by StudyInstanceUID
            var msgIn = new ExtractionRequestMessage();
            msgIn.KeyTag = DicomTag.StudyInstanceUID.DictionaryEntry.Keyword;
            
            //extract only MR (this is what we are actually testing).
            msgIn.Modality = "MR";
            msgIn.ExtractionIdentifiers = studies;

            int matches = 0;
            
            //The strategy pattern implementation that goes to the database but also considers reason

            
            var fulfiller = new FromCataloguesExtractionRequestFulfiller(new[] {cataCT,cataMR})
            {
                //Give it the default modality pattern (table prefix)
                ModalityRoutingRegex = new Regex(new CohortExtractorOptions().ModalityRoutingRegex)
            };
            
            foreach (ExtractImageCollection msgOut in fulfiller.GetAllMatchingFiles(msgIn, new NullAuditExtractions()))
            {
                matches += msgOut.Accepted.Count();
                Assert.IsEmpty(msgOut.Rejected);
            }

            //expect only the MR images to be returned
            Assert.AreEqual(30,matches);


            // Ask for something that doesn't exist
            msgIn.Modality = "Hello";
            var ex = Assert.Throws<Exception>(()=>fulfiller.GetAllMatchingFiles(msgIn, new NullAuditExtractions()).ToArray());
            StringAssert.Contains("Modality=Hello",ex.Message);

            // Ask for all modalities at once by not specifying any
            msgIn.Modality = null;
            Assert.AreEqual(100,fulfiller.GetAllMatchingFiles(msgIn, new NullAuditExtractions()).Sum(r => r.Accepted.Count));
            
            // Ask for both modalities specifically
            msgIn.Modality = "CT,Hello";
            Assert.AreEqual(70,fulfiller.GetAllMatchingFiles(msgIn, new NullAuditExtractions()).Sum(r => r.Accepted.Count));

            // Ask for both modalities specifically
            msgIn.Modality = "CT,MR";
            Assert.AreEqual(100,fulfiller.GetAllMatchingFiles(msgIn, new NullAuditExtractions()).Sum(r => r.Accepted.Count));

            //when we don't have that flag anymore the error should tell us that
            tblCT.DropColumn(tblCT.DiscoverColumn("IsOriginal"));
            msgIn.Modality = "CT,MR";

            ex = Assert.Throws(Is.AssignableTo(typeof(Exception)), () => fulfiller.GetAllMatchingFiles(msgIn, new NullAuditExtractions()).ToArray());
            StringAssert.Contains("IsOriginal",ex.Message);

        }

        /// <summary>
        /// Returns SQL to update the <paramref name="topXRows"/> with the provided SET string
        /// </summary>
        /// <param name="tbl">Table to update</param>
        /// <param name="topXRows">Number of rows to change</param>
        /// <param name="setSql">Set SQL e.g. "Set Col1='fish'"</param>
        /// <returns></returns>
        private string GetUpdateTopXSql(DiscoveredTable tbl, int topXRows, string setSql)
        {
            switch (tbl.Database.Server.DatabaseType)
            {
                case DatabaseType.MicrosoftSQLServer:
                    return
                        $"UPDATE TOP ({topXRows}) {tbl.GetFullyQualifiedName()} {setSql}";
                case DatabaseType.MySql:
                    return
                        $"UPDATE {tbl.GetFullyQualifiedName()} {setSql} LIMIT {topXRows}";
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
        }
    }

    internal class TestRejector : IRejector
    {
        public bool Reject(DbDataReader row, out string reason)
        {
            //if the image is not extractable
            if (!Convert.ToBoolean(row["IsExtractableToDisk"]))
            {
                //tell them why and reject it
                reason = row["IsExtractableToDisk_Reason"] as string;
                return true;
            }

            reason = null;
            return false;
        }
    }
}
