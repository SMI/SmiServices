using NUnit.Framework;
using SmiServices.Applications.ExtractImages;
using SmiServices.Common.Messages.Extraction;
using SmiServices.UnitTests.Common;
using System;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;


namespace SmiServices.UnitTests.Applications.ExtractImages
{
    public class CohortCsvParserTests
    {
        #region Fixture Methods

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() { }

        #endregion

        #region Test Methods

        [SetUp]
        public void SetUp() { }

        [TearDown]
        public void TearDown() { }

        #endregion

        #region Tests

        [TestCase(ExtractionKey.StudyInstanceUID)]
        [TestCase(ExtractionKey.SeriesInstanceUID)]
        [TestCase(ExtractionKey.SOPInstanceUID)]
        public void HappyPath(ExtractionKey expectedExtractionKey)
        {
            var fs = new MockFileSystem(
                new Dictionary<string, MockFileData>
                {
                    {"foo.csv", $"{expectedExtractionKey}\n1.2.3.4"},
                }
            );

            var parser = new CohortCsvParser(fs);
            (ExtractionKey extractionKey, List<string> ids) = parser.Parse("foo.csv");

            Assert.Multiple(() =>
            {
                Assert.That(ids, Is.EqualTo(new List<string> { "1.2.3.4" }));
                Assert.That(extractionKey, Is.EqualTo(expectedExtractionKey));
            });
        }

        [Test]
        public void HappyPath_AFewMore()
        {
            var fs = new MockFileSystem(
                new Dictionary<string, MockFileData>
                {
                    {"foo.csv", "StudyInstanceUID\n1.2.3.4\n5.6.7.8"},
                }
            );

            var parser = new CohortCsvParser(fs);
            (ExtractionKey extractionKey, List<string> ids) = parser.Parse("foo.csv");

            Assert.Multiple(() =>
            {
                Assert.That(extractionKey, Is.EqualTo(ExtractionKey.StudyInstanceUID));
                Assert.That(ids, Is.EqualTo(new List<string> { "1.2.3.4", "5.6.7.8" }));
            });
        }

        [Test]
        public void BlankLines_AreIgnored()
        {
            var fs = new MockFileSystem(
                new Dictionary<string, MockFileData>
                {
                    {"foo.csv", "StudyInstanceUID\n\n1.2.3.4\n\n\n5.6.7.8\n\n\n\n"},
                }
            );

            var parser = new CohortCsvParser(fs);
            (ExtractionKey extractionKey, List<string> ids) = parser.Parse("foo.csv");

            Assert.Multiple(() =>
            {
                Assert.That(extractionKey, Is.EqualTo(ExtractionKey.StudyInstanceUID));
                Assert.That(ids, Is.EqualTo(new List<string> { "1.2.3.4", "5.6.7.8" }));
            });
        }

        [Test]
        public void ExtraWhitespace_IsStripped()
        {
            var fs = new MockFileSystem(
                new Dictionary<string, MockFileData>
                {
                    {"foo.csv", "StudyInstanceUID\n   1.2.3.4     "},
                }
            );

            var parser = new CohortCsvParser(fs);
            (ExtractionKey extractionKey, List<string> ids) = parser.Parse("foo.csv");

            Assert.Multiple(() =>
            {
                Assert.That(extractionKey, Is.EqualTo(ExtractionKey.StudyInstanceUID));
                Assert.That(ids, Is.EqualTo(new List<string> { "1.2.3.4" }));
            });
        }

        [Test]
        public void QuotedValues_AreAllowed()
        {
            var fs = new MockFileSystem(
                new Dictionary<string, MockFileData>
                {
                    {"foo.csv", "StudyInstanceUID\n\"1.2.3.4\""},
                }
            );

            var parser = new CohortCsvParser(fs);
            (ExtractionKey extractionKey, List<string> ids) = parser.Parse("foo.csv");

            Assert.Multiple(() =>
            {
                Assert.That(extractionKey, Is.EqualTo(ExtractionKey.StudyInstanceUID));
                Assert.That(ids, Is.EqualTo(new List<string> { "1.2.3.4" }));
            });
        }

        [Test]
        public void EmptyCsv_ThrowsException()
        {
            var fs = new MockFileSystem(
                new Dictionary<string, MockFileData>
                {
                    {"foo.csv", ""},
                }
            );

            var parser = new CohortCsvParser(fs);

            var exc = Assert.Throws<ApplicationException>(() => parser.Parse("foo.csv"));
            Assert.That(exc!.Message, Is.EqualTo("CSV is empty"));
        }

        [Test]
        public void InvalidHeader_ThrowsException()
        {
            var fs = new MockFileSystem(
                new Dictionary<string, MockFileData>
                {
                    {"foo.csv", "Wheee"},
                }
            );

            var parser = new CohortCsvParser(fs);

            var exc = Assert.Throws<ApplicationException>(() => parser.Parse("foo.csv"));
            Assert.That(exc!.Message, Does.StartWith("CSV header must be a valid ExtractionKey"));
        }

        [Test]
        public void MultiColumn_InHeader_ThrowsException()
        {
            var fs = new MockFileSystem(
                new Dictionary<string, MockFileData>
                {
                    {"foo.csv", "SeriesInstanceUID,"},
                }
            );

            var parser = new CohortCsvParser(fs);

            var exc = Assert.Throws<ApplicationException>(() => parser.Parse("foo.csv"));
            Assert.That(exc!.Message, Is.EqualTo("CSV must have exactly 1 column"));
        }

        [Test]
        public void MultiColumn_InRecord_ThrowsException()
        {
            var fs = new MockFileSystem(
                new Dictionary<string, MockFileData>
                {
                    {"foo.csv", "SeriesInstanceUID\nfoo,"},
                }
            );

            var parser = new CohortCsvParser(fs);

            var exc = Assert.Throws<ApplicationException>(() => parser.Parse("foo.csv"));
            Assert.That(exc!.Message, Is.EqualTo("CSV must have exactly 1 column"));
        }

        [Test]
        public void NoRecords_ThrowsException()
        {
            var fs = new MockFileSystem(
                new Dictionary<string, MockFileData>
                {
                    {"foo.csv", "SeriesInstanceUID\n"},
                }
            );

            var parser = new CohortCsvParser(fs);

            var exc = Assert.Throws<ApplicationException>(() => parser.Parse("foo.csv"));
            Assert.That(exc!.Message, Is.EqualTo("No records in the cohort CSV"));
        }

        #endregion
    }
}
