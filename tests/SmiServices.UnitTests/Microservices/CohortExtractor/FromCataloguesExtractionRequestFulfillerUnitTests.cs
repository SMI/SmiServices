using NUnit.Framework;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Repositories;
using SmiServices.Common.Messages.Extraction;
using SmiServices.Common.Options;
using SmiServices.Microservices.CohortExtractor.RequestFulfillers;
using System;
using System.Linq;

namespace SmiServices.UnitTests.Microservices.CohortExtractor
{
    public class FromCataloguesExtractionRequestFulfillerUnitTests
    {
        [Test]
        public void GetRejectorsFor_NoRejectors()
        {
            CreateCTMR(out ICatalogue ct, out ICatalogue mr);

            var f = new FromCataloguesExtractionRequestFulfiller([ct, mr]);

            var result = f.GetRejectorsFor(
                new ExtractionRequestMessage(),
                new QueryToExecute(
                    new QueryToExecuteColumnSet(ct, null, null, null, null, false), "FF"));

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GetRejectorsFor_OneBasicRejector()
        {
            CreateCTMR(out ICatalogue ct, out ICatalogue mr);

            var f = new FromCataloguesExtractionRequestFulfiller([ct, mr]);
            f.Rejectors.Add(new TestRejector());

            // when we ask for ct to be extracted
            var result = f.GetRejectorsFor(
                new ExtractionRequestMessage(),
                new QueryToExecute(
                    new QueryToExecuteColumnSet(ct, null, null, null, null, false), "FF"));

            // we should see our rejector being used
            Assert.That(result.Single(), Is.InstanceOf<TestRejector>());

            // when we ask for mr to be extracted
            result = f.GetRejectorsFor(
                new ExtractionRequestMessage(),
                new QueryToExecute(
                    new QueryToExecuteColumnSet(mr, null, null, null, null, false), "FF"));

            // we should still see the rejector being used
            Assert.That(result.Single(), Is.InstanceOf<TestRejector>());
        }


        [Test]
        public void ModalitySpecificRejectors_AppliesToModality_AndOverrides()
        {
            CreateCTMR(out ICatalogue ct, out ICatalogue mr);

            var f = new FromCataloguesExtractionRequestFulfiller([ct, mr]);

            // basic rejector
            f.Rejectors.Add(new TestRejector());
            f.ModalitySpecificRejectors.Add(
                new ModalitySpecificRejectorOptions { Modalities = "MR", Overrides = true }, new RejectAll());

            // CT should...
            var result = f.GetRejectorsFor(new ExtractionRequestMessage(), new QueryToExecute(
                    new QueryToExecuteColumnSet(ct, null, null, null, null, false), "FF")
            { Modality = "CT" });

            // run with the basic rejector
            Assert.That(result.Single(), Is.InstanceOf<TestRejector>());

            // MR should...
            result = f.GetRejectorsFor(new ExtractionRequestMessage(), new QueryToExecute(
                    new QueryToExecuteColumnSet(mr, null, null, null, null, false), "FF")
            { Modality = "MR" });

            // use only the modality specific rejector (since it overrides)
            Assert.That(result.Single(), Is.InstanceOf<RejectAll>());
        }


        [Test]
        public void ModalitySpecificRejectors_AppliesToModality_DoesNotOverride()
        {
            CreateCTMR(out ICatalogue ct, out ICatalogue mr);

            var f = new FromCataloguesExtractionRequestFulfiller([ct, mr]);
            IRejector rej1;
            IRejector rej2;

            // basic rejector
            f.Rejectors.Add(rej1 = new TestRejector());
            f.ModalitySpecificRejectors.Add(
                new ModalitySpecificRejectorOptions { Modalities = "MR", Overrides = false }, rej2 = new RejectAll());

            // CT should...
            var result = f.GetRejectorsFor(new ExtractionRequestMessage(), new QueryToExecute(
                    new QueryToExecuteColumnSet(ct, null, null, null, null, false), "FF")
            { Modality = "CT" })
                .ToArray();

            // run with the basic rejector
            Assert.That(result.Single(), Is.InstanceOf<TestRejector>());

            // MR should...
            result = f.GetRejectorsFor(new ExtractionRequestMessage(), new QueryToExecute(
                    new QueryToExecuteColumnSet(mr, null, null, null, null, false), "FF")
            { Modality = "MR" })
                .ToArray();

            // use both the modality specific and the generic rules
            Assert.That(result, Does.Contain(rej1));
            Assert.That(result, Does.Contain(rej2));
        }


        [Test]
        public void ModalitySpecificRejectors_TwoModalities_OneMatches()
        {
            CreateCTMR(out ICatalogue ct, out ICatalogue mr);

            var f = new FromCataloguesExtractionRequestFulfiller([ct, mr]);
            IRejector rej1;
            IRejector rej2;

            // basic rejector
            f.Rejectors.Add(rej1 = new TestRejector());
            f.ModalitySpecificRejectors.Add(
                new ModalitySpecificRejectorOptions { Modalities = "MR,SR", Overrides = true }, rej2 = new RejectAll());

            // CT should...
            var result = f.GetRejectorsFor(new ExtractionRequestMessage(), new QueryToExecute(
                    new QueryToExecuteColumnSet(ct, null, null, null, null, false), "FF")
            { Modality = "CT" })
                .ToArray();

            // run with the basic rejector
            Assert.That(result.Single(), Is.EqualTo(rej1));

            // MR should...
            result = f.GetRejectorsFor(new ExtractionRequestMessage(), new QueryToExecute(
                    new QueryToExecuteColumnSet(mr, null, null, null, null, false), "FF")
            { Modality = "MR" })
                .ToArray();

            // run with the modality specific rejector 
            Assert.That(result.Single(), Is.EqualTo(rej2));
        }


        [Test]
        public void ModalitySpecificRejectors_TwoModalities_BothMatches()
        {
            CreateCTMR(out ICatalogue ct, out ICatalogue mr);

            var f = new FromCataloguesExtractionRequestFulfiller([ct, mr]);
            IRejector rej2;

            // basic rejector
            f.Rejectors.Add(_ = new TestRejector());
            f.ModalitySpecificRejectors.Add(
                new ModalitySpecificRejectorOptions { Modalities = "MR,CT", Overrides = true }, rej2 = new RejectAll());

            // CT should...
            var result = f.GetRejectorsFor(new ExtractionRequestMessage(), new QueryToExecute(
                    new QueryToExecuteColumnSet(ct, null, null, null, null, false), "FF")
            { Modality = "CT" })
                .ToArray();

            // run with the modality specific rejector 
            Assert.That(result.Single(), Is.EqualTo(rej2));

            // MR should...
            result = f.GetRejectorsFor(new ExtractionRequestMessage(), new QueryToExecute(
                    new QueryToExecuteColumnSet(mr, null, null, null, null, false), "FF")
            { Modality = "MR" })
                .ToArray();

            // run with the modality specific rejector 
            Assert.That(result.Single(), Is.EqualTo(rej2));
        }

        [Test]
        public void ModalitySpecificRejectors_MixingOverrideRules()
        {
            CreateCTMR(out ICatalogue ct, out ICatalogue mr);

            var f = new FromCataloguesExtractionRequestFulfiller([ct, mr]);
            IRejector rej1;
            IRejector rej2;

            // basic rejector
            f.Rejectors.Add(rej1 = new TestRejector());

            // two rules for MR but one says to override while other says not to!
            f.ModalitySpecificRejectors.Add(
                new ModalitySpecificRejectorOptions { Modalities = "MR", Overrides = false }, rej2 = new RejectAll());
            f.ModalitySpecificRejectors.Add(
                new ModalitySpecificRejectorOptions { Modalities = "MR", Overrides = true }, rej2 = new RejectAll());

            // CT should...
            var result = f.GetRejectorsFor(new ExtractionRequestMessage(), new QueryToExecute(
                    new QueryToExecuteColumnSet(ct, null, null, null, null, false), "FF")
            { Modality = "CT" })
                .ToArray();

            // run with the basic rejector
            Assert.That(result.Single(), Is.InstanceOf<TestRejector>());

            // MR should...
            var ex = Assert.Throws<Exception>(() => f.GetRejectorsFor(new ExtractionRequestMessage(), new QueryToExecute(
                    new QueryToExecuteColumnSet(mr, null, null, null, null, false), "FF")
            { Modality = "MR" })
                .ToArray());

            Assert.That(ex!.Message, Is.EqualTo("You cannot mix Overriding and non Overriding ModalitySpecificRejectors.  Bad Modality was 'MR'"));
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
