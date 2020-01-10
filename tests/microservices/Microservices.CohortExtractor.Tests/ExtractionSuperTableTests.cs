using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BadMedicine;
using BadMedicine.Dicom;
using Dicom;
using FAnsi.Discovery;
using Microservices.CohortExtractor.Execution.RequestFulfillers;
using NUnit.Framework;
using NUnit.Framework.Internal;
using Rdmp.Core.DataLoad.Triggers;
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


        [TestCase(DatabaseType.MicrosoftSQLServer), RequiresRelationalDb(DatabaseType.MicrosoftSQLServer)]
        [TestCase(DatabaseType.MySql), RequiresRelationalDb(DatabaseType.MySql)]
        public void Test_OnlyExtractableImages(DatabaseType dbType)
        {
            var db = GetCleanedServer(dbType);

            var tbl = BuildExampleExtractionTable(db, "CT", 100,true);

            Assert.AreEqual(100,tbl.GetRowCount());



            //IExtractionRequestFulfiller fulfiller = new FromCataloguesExtractionRequestFulfiller();
        }
    }
}
