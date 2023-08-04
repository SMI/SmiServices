using System;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using NUnit.Framework;
using Smi.Common.Messages.Extraction;
using Smi.Common.Tests;


namespace Applications.ExtractImages.Tests
{
    public class CohortCsvParserTests
    {
        #region Fixture Methods

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestLogger.Setup();
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

            Assert.AreEqual(extractionKey, extractionKey);
            Assert.AreEqual(new List<string> { "1.2.3.4" }, ids);
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

            Assert.AreEqual(ExtractionKey.StudyInstanceUID, extractionKey);
            Assert.AreEqual(new List<string> { "1.2.3.4", "5.6.7.8" }, ids);
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

            Assert.AreEqual(ExtractionKey.StudyInstanceUID, extractionKey);
            Assert.AreEqual(new List<string> { "1.2.3.4", "5.6.7.8" }, ids);
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

            Assert.AreEqual(ExtractionKey.StudyInstanceUID, extractionKey);
            Assert.AreEqual(new List<string> { "1.2.3.4" }, ids);
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

            Assert.AreEqual(ExtractionKey.StudyInstanceUID, extractionKey);
            Assert.AreEqual(new List<string> { "1.2.3.4" }, ids);
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
            Assert.AreEqual("CSV is empty", exc!.Message);
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
            Assert.True(exc!.Message.StartsWith("CSV header must be a valid ExtractionKey"));
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
            Assert.AreEqual("CSV must have exactly 1 column", exc!.Message);
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
            Assert.AreEqual("CSV must have exactly 1 column", exc!.Message);
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
            Assert.AreEqual("No records in the cohort CSV", exc!.Message);
        }

        #endregion
    }
}
