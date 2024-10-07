
using FAnsi;
using FAnsi.Discovery;
using FAnsi.Extensions;
using NUnit.Framework;
using Rdmp.Core.Curation.Data;
using SmiServices.Common.Messages.Extraction;
using SmiServices.Microservices.CohortExtractor;
using SmiServices.Microservices.CohortExtractor.Audit;
using SmiServices.Microservices.CohortExtractor.RequestFulfillers;
using SmiServices.UnitTests.Common;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Tests.Common;

namespace SmiServices.IntegrationTests.Microservices.CohortExtractor
{
    /// <summary>
    /// Tests the ability of <see cref="FromCataloguesExtractionRequestFulfiller"/> to connect to a database
    /// (described in a <see cref="Catalogue"/>) and fetch matching image urls out of the database (creating
    /// ExtractImageCollection results).
    /// </summary>
    [RequiresRelationalDb(DatabaseType.MySql)]
    [RequiresRelationalDb(DatabaseType.MicrosoftSQLServer)]
    public class FromCataloguesExtractionRequestFulfillerTests : DatabaseTests
    {
        [SetUp]
        protected override void SetUp()
        {
            base.SetUp();

            TestLogger.Setup();
        }

        [TestCase(DatabaseType.MicrosoftSQLServer)]
        [TestCase(DatabaseType.MySql)]
        public void FromCataloguesExtractionRequestFulfiller_NormalMatching_SeriesInstanceUIDOnly(DatabaseType databaseType)
        {
            var db = GetCleanedServer(databaseType);

            var dt = new DataTable();
            dt.Columns.Add("SeriesInstanceUID");
            dt.Columns.Add("Extractable", typeof(bool));
            dt.Columns.Add(QueryToExecuteColumnSet.DefaultImagePathColumnName);

            dt.Rows.Add("123", true, "/images/1.dcm");
            dt.Rows.Add("123", false, "/images/2.dcm");
            dt.Rows.Add("1234", false, "/images/3.dcm");
            dt.Rows.Add("1234", true, "/images/4.dcm");

            dt.SetDoNotReType(true);

            var tbl = db.CreateTable("FromCataloguesExtractionRequestFulfillerTests", dt);
            var catalogue = Import(tbl);

            var fulfiller = new FromCataloguesExtractionRequestFulfiller([catalogue]);

            var matching = fulfiller.GetAllMatchingFiles(new ExtractionRequestMessage
            {
                KeyTag = "SeriesInstanceUID",
                ExtractionIdentifiers = new List<string>(["123"]),
            }, new NullAuditExtractions()).ToArray();

            Assert.That(matching, Has.Length.EqualTo(1));
            Assert.Multiple(() =>
            {
                Assert.That(matching[0].Accepted, Has.Count.EqualTo(2));
                Assert.That(matching[0].Accepted.Count(f => f.FilePathValue.Equals("/images/1.dcm")), Is.EqualTo(1));
                Assert.That(matching[0].Accepted.Count(f => f.FilePathValue.Equals("/images/2.dcm")), Is.EqualTo(1));
            });
        }

        [TestCase(DatabaseType.MicrosoftSQLServer)]
        [TestCase(DatabaseType.MySql)]
        public void FromCataloguesExtractionRequestFulfiller_MandatoryFilter_SeriesInstanceUIDOnly(DatabaseType databaseType)
        {
            var db = GetCleanedServer(databaseType);

            var dt = new DataTable();
            dt.Columns.Add("SeriesInstanceUID");
            dt.Columns.Add("Extractable", typeof(bool));
            dt.Columns.Add(QueryToExecuteColumnSet.DefaultImagePathColumnName);

            dt.Rows.Add("123", true, "/images/1.dcm");
            dt.Rows.Add("123", false, "/images/2.dcm");
            dt.Rows.Add("1234", false, "/images/3.dcm");
            dt.Rows.Add("1234", true, "/images/4.dcm");

            dt.SetDoNotReType(true);

            var tbl = db.CreateTable("FromCataloguesExtractionRequestFulfillerTests", dt);
            var catalogue = Import(tbl);

            var ei = catalogue.GetAllExtractionInformation(ExtractionCategory.Any).First();
            var filter = new ExtractionFilter(CatalogueRepository, "Extractable only", ei)
            {
                IsMandatory = true,
                WhereSQL = "Extractable = 1"
            };
            filter.SaveToDatabase();
            var fulfiller = new FromCataloguesExtractionRequestFulfiller([catalogue]);

            var matching = fulfiller.GetAllMatchingFiles(new ExtractionRequestMessage
            {
                KeyTag = "SeriesInstanceUID",
                ExtractionIdentifiers = new List<string>(["123"]),
            }, new NullAuditExtractions()).ToArray();

            Assert.That(matching, Has.Length.EqualTo(1));
            Assert.That(matching[0].Accepted, Has.Count.EqualTo(1));
            Assert.That(matching[0].Accepted.Count(f => f.FilePathValue.Equals("/images/1.dcm")), Is.EqualTo(1));
        }


        [TestCase(DatabaseType.MicrosoftSQLServer)]
        [TestCase(DatabaseType.MySql)]
        public void FromCataloguesExtractionRequestFulfiller_NormalMatching(DatabaseType databaseType)
        {
            var db = GetCleanedServer(databaseType);

            var dt = new DataTable();
            dt.Columns.Add("StudyInstanceUID");
            dt.Columns.Add("SeriesInstanceUID");
            dt.Columns.Add("SOPInstanceUID");
            dt.Columns.Add("Extractable");
            dt.Columns.Add(QueryToExecuteColumnSet.DefaultImagePathColumnName);

            dt.Rows.Add("1.1", "123.1", "1.1", true, "/images/1.dcm");
            dt.Rows.Add("1.1", "123.1", "2.1", false, "/images/2.dcm");
            dt.Rows.Add("1.1", "1234.1", "3.1", false, "/images/3.dcm");
            dt.Rows.Add("1.1", "1234.1", "4.1", true, "/images/4.dcm");

            dt.SetDoNotReType(true);

            var tbl = db.CreateTable("FromCataloguesExtractionRequestFulfillerTests", dt);
            var catalogue = Import(tbl);

            var fulfiller = new FromCataloguesExtractionRequestFulfiller([catalogue]);

            var matching = fulfiller.GetAllMatchingFiles(new ExtractionRequestMessage
            {
                KeyTag = "SeriesInstanceUID",
                ExtractionIdentifiers = new List<string>(["123.1"]),
            }, new NullAuditExtractions()).ToArray();

            Assert.That(matching, Has.Length.EqualTo(1));
            Assert.That(matching[0].Accepted, Has.Count.EqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(matching[0].Accepted.Count(f => f.FilePathValue.Equals("/images/1.dcm")), Is.EqualTo(1));
                Assert.That(matching[0].Accepted.Count(f => f.FilePathValue.Equals("/images/2.dcm")), Is.EqualTo(1));
            });
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
            dt.Columns.Add("Extractable", typeof(bool));
            dt.Columns.Add(QueryToExecuteColumnSet.DefaultImagePathColumnName);

            dt.Rows.Add("1.1", "123.1", "1.1", true, "/images/1.dcm");
            dt.Rows.Add("1.1", "123.1", "2.1", false, "/images/2.dcm");
            dt.Rows.Add("1.1", "1234.1", "3.1", false, "/images/3.dcm");
            dt.Rows.Add("1.1", "1234.1", "4.1", true, "/images/4.dcm");

            dt.SetDoNotReType(true);

            var tbl = db.CreateTable("FromCataloguesExtractionRequestFulfillerTests", dt);
            var catalogue = Import(tbl);

            var ei = catalogue.GetAllExtractionInformation(ExtractionCategory.Any).First();
            var filter = new ExtractionFilter(CatalogueRepository, "Extractable only", ei)
            {
                IsMandatory = true,
                WhereSQL = "Extractable = 1"
            };
            filter.SaveToDatabase();
            var fulfiller = new FromCataloguesExtractionRequestFulfiller([catalogue]);

            var matching = fulfiller.GetAllMatchingFiles(new ExtractionRequestMessage
            {
                KeyTag = "SeriesInstanceUID",
                ExtractionIdentifiers = new List<string>(["123.1"]),
            }, new NullAuditExtractions()).ToArray();

            Assert.That(matching, Has.Length.EqualTo(1));
            Assert.That(matching[0].Accepted, Has.Count.EqualTo(1));
            Assert.That(matching[0].Accepted.Count(f => f.FilePathValue.Equals("/images/1.dcm")), Is.EqualTo(1));
        }

        [TestCase(DatabaseType.MicrosoftSQLServer, true)]
        [TestCase(DatabaseType.MicrosoftSQLServer, false)]
        public void Test_FromCataloguesExtractionRequestFulfiller_NoFilterExtraction(DatabaseType databaseType, bool isNoFiltersExtraction)
        {
            DiscoveredDatabase db = GetCleanedServer(databaseType);

            var dt = new DataTable();
            dt.Columns.Add("StudyInstanceUID");
            dt.Columns.Add("SeriesInstanceUID");
            dt.Columns.Add("SOPInstanceUID");
            dt.Columns.Add("Extractable", typeof(bool));
            dt.Columns.Add(QueryToExecuteColumnSet.DefaultImagePathColumnName);
            dt.Rows.Add("1.1", "123.1", "1.1", true, "/images/1.dcm");
            dt.SetDoNotReType(true);

            DiscoveredTable tbl = db.CreateTable("FromCataloguesExtractionRequestFulfillerTests", dt);
            ICatalogue catalogue = Import(tbl);

            ExtractionInformation ei = catalogue.GetAllExtractionInformation(ExtractionCategory.Any).First();
            var filter = new ExtractionFilter(CatalogueRepository, "Extractable only", ei)
            {
                IsMandatory = true,
                WhereSQL = "Extractable = 1"
            };
            filter.SaveToDatabase();
            var fulfiller = new FromCataloguesExtractionRequestFulfiller([catalogue]);
            fulfiller.Rejectors.Add(new RejectAll());

            var message = new ExtractionRequestMessage
            {
                KeyTag = "SeriesInstanceUID",
                ExtractionIdentifiers = new List<string>(["123.1"]),
                IsNoFilterExtraction = isNoFiltersExtraction,
            };

            ExtractImageCollection[] matching = fulfiller.GetAllMatchingFiles(message, new NullAuditExtractions()).ToArray();

            int expected = isNoFiltersExtraction ? 1 : 0;
            Assert.That(matching, Has.Length.EqualTo(1));
            Assert.That(matching[0].Accepted, Has.Count.EqualTo(expected));
        }
    }
}
