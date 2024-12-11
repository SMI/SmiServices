using Moq;
using NUnit.Framework;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Repositories;
using SmiServices.Common;
using SmiServices.Common.Messages.Extraction;
using SmiServices.Common.Options;
using SmiServices.Microservices.CohortExtractor;
using SmiServices.Microservices.CohortExtractor.RequestFulfillers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;

namespace SmiServices.UnitTests.Microservices.CohortExtractor
{
    public class FromCataloguesExtractionRequestFulfillerUnitTests
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            FansiImplementations.Load();
        }

        [Test]
        public void Constructor_HappyPath()
        {
            // Arrange

            var catalogue = CreateCatalogue("CT");

            // Act

            FromCataloguesExtractionRequestFulfiller call() => new([catalogue]);

            // Assert

            Assert.DoesNotThrow(() => call());
        }

        [Test]
        public void Constructor_NoCompatibleCatalogues_Throws()
        {
            // Arrange

            var mockCatalogue = new Mock<ICatalogue>(MockBehavior.Strict);
            mockCatalogue.Setup(x => x.ID).Returns(1);
            mockCatalogue.Setup(x => x.GetAllExtractionInformation(It.IsAny<ExtractionCategory>())).Returns([]);

            // Act

            FromCataloguesExtractionRequestFulfiller call() => new([mockCatalogue.Object]);

            // Assert

            var exc = Assert.Throws<ArgumentOutOfRangeException>(() => call());
            Assert.That(exc.Message, Is.EqualTo("There are no compatible Catalogues in the repository (See QueryToExecuteColumnSet for required columns) (Parameter 'cataloguesToUseForImageLookup')"));
        }

        [TestCase("(.)_(.)")]
        [TestCase("._.")]
        public void Constructor_InvalidRegex_Throws(string regexString)
        {
            // Arrange

            var catalogue = CreateCatalogue("CT");

            // Act

            FromCataloguesExtractionRequestFulfiller call() => new([catalogue], new Regex(regexString));

            // Assert

            var exc = Assert.Throws<ArgumentOutOfRangeException>(() => call());
            Assert.That(exc.Message, Is.EqualTo("Must have exactly one non-default capture group (Parameter 'modalityRoutingRegex')"));
        }

        [Test]
        public void GetAllMatchingFiles_MatchingModalityNoFiles_ReturnsEmpty()
        {
            // Arrange

            var catalogue = CreateCatalogue("CT");

            var message = new ExtractionRequestMessage
            {
                KeyTag = "SeriesInstanceUID",
                Modality = "CT",
            };

            var fulfiller = new FromCataloguesExtractionRequestFulfiller([catalogue]);

            // Act

            var files = fulfiller.GetAllMatchingFiles(message);

            // Assert

            Assert.That(files.ToList(), Is.Empty);
        }

        [Test]
        public void GetAllMatchingFiles_NoCatalogueForModality_Throws()
        {
            // Arrange

            var catalogue = CreateCatalogue("CT");

            var message = new ExtractionRequestMessage
            {
                KeyTag = "SeriesInstanceUID",
                Modality = "MR",
            };

            var fulfiller = new FromCataloguesExtractionRequestFulfiller([catalogue]);

            // Act

            List<ExtractImageCollection> call() => fulfiller.GetAllMatchingFiles(message).ToList();

            // Assert

            var exc = Assert.Throws<Exception>(() => call().ToList());
            Assert.That(exc!.Message, Does.StartWith("Couldn't find any compatible Catalogues to run extraction queries against for query"));
        }

        [Test]
        public void GetAllMatchingFiles_NonMixedOverridingRejectors_Passes()
        {
            // Arrange

            var catalogue = CreateCatalogue("CT");

            var message = new ExtractionRequestMessage
            {
                KeyTag = "SeriesInstanceUID",
                Modality = "CT",
            };

            var fulfiller = new FromCataloguesExtractionRequestFulfiller([catalogue]);
            fulfiller.ModalitySpecificRejectors.Add(
                new ModalitySpecificRejectorOptions
                {
                    Overrides = true,
                    Modalities = "CT",
                },
                new RejectNone()
            );
            fulfiller.ModalitySpecificRejectors.Add(
                new ModalitySpecificRejectorOptions
                {
                    Overrides = true,
                    Modalities = "CT",
                },
                new RejectNone()
            );

            // Act

            List<ExtractImageCollection> call() => fulfiller.GetAllMatchingFiles(message).ToList();

            // Assert

            Assert.DoesNotThrow(() => call().ToList());
        }

        [Test]
        public void GetAllMatchingFiles_MixedOverridingRejectors_Throws()
        {
            // Arrange

            var catalogue = CreateCatalogue("CT");

            var message = new ExtractionRequestMessage
            {
                KeyTag = "SeriesInstanceUID",
                Modality = "CT",
            };

            var fulfiller = new FromCataloguesExtractionRequestFulfiller([catalogue]);
            fulfiller.ModalitySpecificRejectors.Add(
                new ModalitySpecificRejectorOptions
                {
                    Overrides = true,
                    Modalities = "CT",
                },
                new RejectNone()
            );
            fulfiller.ModalitySpecificRejectors.Add(
                new ModalitySpecificRejectorOptions
                {
                    Overrides = false,
                    Modalities = "CT",
                },
                new RejectNone()
            );

            // Act

            List<ExtractImageCollection> call() => fulfiller.GetAllMatchingFiles(message).ToList();

            // Assert

            var exc = Assert.Throws<Exception>(() => call().ToList());
            Assert.That(exc!.Message, Is.EqualTo("You cannot mix Overriding and non Overriding ModalitySpecificRejectors. Bad Modality was 'CT'"));
        }

        private static ICatalogue CreateCatalogue(string modality)
        {
            var memoryRepo = new MemoryCatalogueRepository();
            var catalogue = new Catalogue(memoryRepo, $"{modality}_ImageTable");
            Add(catalogue, "RelativeFileArchiveURI");
            Add(catalogue, "StudyInstanceUID");
            Add(catalogue, "SeriesInstanceUID");
            Add(catalogue, "SOPInstanceUID");
            return catalogue;
        }

        private static void Add(ICatalogue c, string col)
        {
            var repo = c.CatalogueRepository;
            var ci = new CatalogueItem(repo, c, col);
            var ti = new TableInfo(repo, "ff")
            {
                Server = "ff",
                Database = "db",
            };
            _ = new ExtractionInformation(repo, ci, new ColumnInfo(repo, col, "varchar(10)", ti), col);
        }

        private class RejectNone : IRejector
        {
            public bool Reject(IDataRecord row, [NotNullWhen(true)] out string? reason)
            {
                reason = null;
                return false;
            }
        }
    }
}
