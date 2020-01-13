
using FAnsi;
using Microservices.CohortExtractor.Audit;
using Microservices.CohortExtractor.Execution.RequestFulfillers;
using NUnit.Framework;
using Rdmp.Core.Curation.Data;
using Smi.Common.Messages.Extraction;
using Smi.Common.Tests;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using FAnsi.Extensions;
using Tests.Common;

namespace Microservices.CohortExtractor.Tests
{
    /// <summary>
    /// Tests the ability of <see cref="FromCataloguesExtractionRequestFulfiller"/> to connect to a database
    /// (described in a <see cref="Catalogue"/>) and fetch matching image urls out of the database (creating
    /// ExtractImageCollection results).
    /// </summary>
    class FromCataloguesExtractionRequestFulfillerTests : DatabaseTests
    {

        [TestCase(DatabaseType.MicrosoftSQLServer), RequiresRelationalDb(DatabaseType.MicrosoftSQLServer)]
        [TestCase(DatabaseType.MySql), RequiresRelationalDb(DatabaseType.MySql)]
        public void FromCataloguesExtractionRequestFulfiller_NormalMatching_SeriesInstanceUIDOnly(DatabaseType databaseType)
        {
            var db = GetCleanedServer(databaseType);

            var dt = new DataTable();
            dt.Columns.Add("SeriesInstanceUID");
            dt.Columns.Add("Extractable",typeof(bool));
            dt.Columns.Add(QueryToExecuteColumnSet.DefaultImagePathColumnName);

            dt.Rows.Add("123", true, "/images/1.dcm");
            dt.Rows.Add("123", false, "/images/2.dcm");
            dt.Rows.Add("1234", false, "/images/3.dcm");
            dt.Rows.Add("1234", true, "/images/4.dcm");

            dt.SetDoNotReType(true);

            var tbl = db.CreateTable("FromCataloguesExtractionRequestFulfillerTests", dt);
            var catalogue = Import(tbl);

            var fulfiller = new FromCataloguesExtractionRequestFulfiller(new[] { catalogue });

            var matching = fulfiller.GetAllMatchingFiles(new ExtractionRequestMessage()
            {
                KeyTag = "SeriesInstanceUID",
                ExtractionIdentifiers = new List<string>(new string[] { "123" }),
            }, new NullAuditExtractions()).ToArray();

            Assert.AreEqual(1, matching.Length);
            Assert.AreEqual(2, matching[0].Accepted.Count());
            Assert.AreEqual(1, matching[0].Accepted.Count(f => f.FilePathValue.Equals("/images/1.dcm")));
            Assert.AreEqual(1, matching[0].Accepted.Count(f => f.FilePathValue.Equals("/images/2.dcm")));
        }
        
        [TestCase(DatabaseType.MicrosoftSQLServer)]
        [TestCase(DatabaseType.MySql)]
        public void FromCataloguesExtractionRequestFulfiller_MandatoryFilter_SeriesInstanceUIDOnly(DatabaseType databaseType)
        {
            var db = GetCleanedServer(databaseType);

            var dt = new DataTable();
            dt.Columns.Add("SeriesInstanceUID");
            dt.Columns.Add("Extractable",typeof(bool));
            dt.Columns.Add(QueryToExecuteColumnSet.DefaultImagePathColumnName);

            dt.Rows.Add("123", true, "/images/1.dcm");
            dt.Rows.Add("123", false, "/images/2.dcm");
            dt.Rows.Add("1234", false, "/images/3.dcm");
            dt.Rows.Add("1234", true, "/images/4.dcm");

            dt.SetDoNotReType(true);

            var tbl = db.CreateTable("FromCataloguesExtractionRequestFulfillerTests", dt);
            var catalogue = Import(tbl);

            var ei = catalogue.GetAllExtractionInformation(ExtractionCategory.Any).First();
            var filter = new ExtractionFilter(CatalogueRepository, "Extractable only", ei);
            filter.IsMandatory = true;
            filter.WhereSQL = "Extractable = 1";
            filter.SaveToDatabase();
            var fulfiller = new FromCataloguesExtractionRequestFulfiller(new[] { catalogue });

            var matching = fulfiller.GetAllMatchingFiles(new ExtractionRequestMessage()
            {
                KeyTag = "SeriesInstanceUID",
                ExtractionIdentifiers = new List<string>(new string[] { "123" }),
            }, new NullAuditExtractions()).ToArray();

            Assert.AreEqual(1, matching.Length);
            Assert.AreEqual(1, matching[0].Accepted.Count);
            Assert.AreEqual(1, matching[0].Accepted.Count(f => f.FilePathValue.Equals("/images/1.dcm")));
        }
    

        [TestCase(DatabaseType.MicrosoftSQLServer), RequiresRelationalDb(DatabaseType.MicrosoftSQLServer)]
        [TestCase(DatabaseType.MySql), RequiresRelationalDb(DatabaseType.MySql)]
        public void FromCataloguesExtractionRequestFulfiller_NormalMatching(DatabaseType databaseType)
        {
            var db = GetCleanedServer(databaseType);

            var dt = new DataTable();
            dt.Columns.Add("StudyInstanceUID");
            dt.Columns.Add("SeriesInstanceUID");
            dt.Columns.Add("SOPInstanceUID");
            dt.Columns.Add("Extractable");
            dt.Columns.Add(QueryToExecuteColumnSet.DefaultImagePathColumnName);

            dt.Rows.Add("1.1","123.1","1.1", true, "/images/1.dcm");
            dt.Rows.Add("1.1","123.1","2.1", false, "/images/2.dcm");
            dt.Rows.Add("1.1","1234.1","3.1", false, "/images/3.dcm");
            dt.Rows.Add("1.1","1234.1","4.1", true, "/images/4.dcm");

            dt.SetDoNotReType(true);

            var tbl = db.CreateTable("FromCataloguesExtractionRequestFulfillerTests", dt);
            var catalogue = Import(tbl);

            var fulfiller = new FromCataloguesExtractionRequestFulfiller(new[] { catalogue });

            var matching = fulfiller.GetAllMatchingFiles(new ExtractionRequestMessage()
            {
                KeyTag = "SeriesInstanceUID",
                ExtractionIdentifiers = new List<string>(new string[] { "123.1" }),
            }, new NullAuditExtractions()).ToArray();

            Assert.AreEqual(1, matching.Length);
            Assert.AreEqual(2, matching[0].Accepted.Count);
            Assert.AreEqual(1, matching[0].Accepted.Count(f => f.FilePathValue.Equals("/images/1.dcm")));
            Assert.AreEqual(1, matching[0].Accepted.Count(f => f.FilePathValue.Equals("/images/2.dcm")));
        }
        [TestCase(DatabaseType.MicrosoftSQLServer)]
        [TestCase(DatabaseType.MySql)]
        public void FromCataloguesExtractionRequestFulfiller_MandatoryFilter(DatabaseType databaseType)
        {
            var db = GetCleanedServer(databaseType);

            var dt = new DataTable();
            dt.Columns.Add("StudyInstanceUID");
            dt.Columns.Add("SeriesInstanceUID");
            dt.Columns.Add("SOPInstanceUID");
            dt.Columns.Add("Extractable",typeof(bool));
            dt.Columns.Add(QueryToExecuteColumnSet.DefaultImagePathColumnName);

            dt.Rows.Add("1.1","123.1","1.1", true, "/images/1.dcm");
            dt.Rows.Add("1.1","123.1","2.1", false, "/images/2.dcm");
            dt.Rows.Add("1.1","1234.1","3.1", false, "/images/3.dcm");
            dt.Rows.Add("1.1","1234.1","4.1", true, "/images/4.dcm");

            dt.SetDoNotReType(true);

            var tbl = db.CreateTable("FromCataloguesExtractionRequestFulfillerTests", dt);
            var catalogue = Import(tbl);

            var ei = catalogue.GetAllExtractionInformation(ExtractionCategory.Any).First();
            var filter = new ExtractionFilter(CatalogueRepository, "Extractable only", ei);
            filter.IsMandatory = true;
            filter.WhereSQL = "Extractable = 1";
            filter.SaveToDatabase();
            var fulfiller = new FromCataloguesExtractionRequestFulfiller(new[] { catalogue });

            var matching = fulfiller.GetAllMatchingFiles(new ExtractionRequestMessage()
            {
                KeyTag = "SeriesInstanceUID",
                ExtractionIdentifiers = new List<string>(new string[] { "123.1" }),
            }, new NullAuditExtractions()).ToArray();

            Assert.AreEqual(1, matching.Length);
            Assert.AreEqual(1, matching[0].Accepted.Count);
            Assert.AreEqual(1, matching[0].Accepted.Count(f => f.FilePathValue.Equals("/images/1.dcm")));
        }
    }
}
