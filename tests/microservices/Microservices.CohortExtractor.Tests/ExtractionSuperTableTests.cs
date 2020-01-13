using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using BadMedicine;
using BadMedicine.Dicom;
using Dicom;
using FAnsi.Discovery;
using FAnsi.Discovery.QuerySyntax.Update;
using Microservices.CohortExtractor.Audit;
using Microservices.CohortExtractor.Execution;
using Microservices.CohortExtractor.Execution.RequestFulfillers;
using NUnit.Framework;
using NUnit.Framework.Internal;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.Spontaneous;
using Rdmp.Core.DataLoad.Triggers;
using Rdmp.Core.Repositories;
using Smi.Common.Messages.Extraction;
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

            var cata = Import(tbl);

            var fufiller = new FromCataloguesExtractionRequestFulfillerWithReason(new[] {cata});
            
            List<string> studies;

            using(var dt = tbl.GetDataTable())
                studies = dt.Rows.Cast<DataRow>().Select(r => r["StudyInstanceUID"]).Cast<string>().Distinct().ToList();

            var msgIn = new ExtractionRequestMessage();
            msgIn.KeyTag = DicomTag.StudyInstanceUID.DictionaryEntry.Keyword;
            msgIn.ExtractionIdentifiers = studies;

            int matches = 0;

            foreach (ExtractImageCollection msgOut in fufiller.GetAllMatchingFiles(msgIn, new NullAuditExtractions()))
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

            foreach (ExtractImageCollection msgOut in fufiller.GetAllMatchingFiles(msgIn, new NullAuditExtractions()))
            {
                matches += msgOut.Accepted.Count;
                rejections += msgOut.Rejected.Count;

                Assert.IsTrue(msgOut.Rejected.All(v=>v.RejectReason.Equals("We decided NO!")));
            }

            Assert.AreEqual(90,matches);
            
            //TODO: we need to capture this
            //Assert.AreEqual(10, rejections);

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

    internal class FromCataloguesExtractionRequestFulfillerWithReason : FromCataloguesExtractionRequestFulfiller
    {
        public FromCataloguesExtractionRequestFulfillerWithReason(Catalogue[] catalogues):base(catalogues)
        {
            
        }

        protected override QueryToExecute GetQueryToExecute(QueryToExecuteColumnSet columnSet, ExtractionRequestMessage message)
        {
            return new QueryToExecuteWithReason(columnSet, message);
        }
    }

    internal class QueryToExecuteWithReason : QueryToExecute
    {
        public QueryToExecuteWithReason(QueryToExecuteColumnSet columnSet, ExtractionRequestMessage message):base(columnSet,message.KeyTag)
        {
            
        }

        protected override IEnumerable<IFilter> GetFilters(MemoryCatalogueRepository memoryRepo, IContainer rootContainer)
        {
            return base.GetFilters(memoryRepo, rootContainer).Union(
            
            new []{new SpontaneouslyInventedFilter(memoryRepo,rootContainer,"IsExtractableToDisk = 1","ExtractableOnly","",null)});
        }
    }
}
