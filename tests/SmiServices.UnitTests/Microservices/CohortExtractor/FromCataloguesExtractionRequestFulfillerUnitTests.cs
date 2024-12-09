using NUnit.Framework;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Repositories;
using SmiServices.Microservices.CohortExtractor.RequestFulfillers;

namespace SmiServices.UnitTests.Microservices.CohortExtractor
{
    public class FromCataloguesExtractionRequestFulfillerUnitTests
    {
        // TODO
        [Test]
        public void Foo()
        {

        }

        private static void CreateCTMR(out ICatalogue ct, out ICatalogue mr)
        {
            var mem = new MemoryCatalogueRepository();

            ct = new Catalogue(mem, "CT_Image");
            Add(ct, QueryToExecuteColumnSet.DefaultImagePathColumnName);
            Add(ct, QueryToExecuteColumnSet.DefaultStudyIdColumnName);
            Add(ct, QueryToExecuteColumnSet.DefaultSeriesIdColumnName);
            Add(ct, QueryToExecuteColumnSet.DefaultInstanceIdColumnName);

            mr = new Catalogue(mem, "MR_Image");
            Add(mr, QueryToExecuteColumnSet.DefaultImagePathColumnName);
            Add(mr, QueryToExecuteColumnSet.DefaultStudyIdColumnName);
            Add(mr, QueryToExecuteColumnSet.DefaultSeriesIdColumnName);
            Add(mr, QueryToExecuteColumnSet.DefaultInstanceIdColumnName);

        }

        private static void Add(ICatalogue c, string col)
        {
            var repo = c.CatalogueRepository;
            var ci = new CatalogueItem(repo, c, col);
            var ti = new TableInfo(repo, "ff")
            {
                Server = "ff",
                Database = "db"
            };
            _ = new ExtractionInformation(repo, ci, new ColumnInfo(repo, col, "varchar(10)", ti), col);
        }
    }
}
