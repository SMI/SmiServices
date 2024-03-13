using Microservices.CohortPackager.Execution.JobProcessing.Reporting.CsvRecords;
using NUnit.Framework;
using Smi.Common.Tests;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Microservices.CohortPackager.Tests.Execution.JobProcessing.Reporting.CsvRecords
{
    public class TagDataFullCsvRecordTest
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

        // tagName
        [TestCase(null, "foo", "foo")]
        [TestCase("", "foo", "foo")]
        // failureValue
        [TestCase("foo", null, "foo")]
        [TestCase("foo", "", "foo")]
        // filePath
        [TestCase("foo", "foo", null)]
        [TestCase("foo", "foo", "")]
        public void Constructor_ThrowsArgumentException_OnInvalidArgs(string? tagName, string? failureValue, string? filePath)
        {
            Assert.Throws<ArgumentException>(() => { var _ = new TagDataFullCsvRecord(tagName, failureValue, filePath); });
        }

        [Test]
        public void BuildRecordList_Empty()
        {
            IEnumerable<TagDataFullCsvRecord> records = TagDataFullCsvRecord.BuildRecordList("foo", new Dictionary<string, List<string>>());
            Assert.That(records, Is.EqualTo(Enumerable.Empty<TagDataFullCsvRecord>()));
        }

        [Test]
        public void BuildRecordList_WithData()
        {
            var testData = new Dictionary<string, List<string>>
            {
                {
                    "foo",
                    new List<string>
                    {
                        "2.dcm",
                        "1.dcm",
                    }
                },
                {
                    "bar",
                    new List<string>
                    {
                        "3.dcm",
                        "2.dcm",
                        "1.dcm",
                    }
                },
            };

            var expected = new List<TagDataFullCsvRecord>
            {
                new TagDataFullCsvRecord("ScanOptions", "bar", "1.dcm"),
                new TagDataFullCsvRecord("ScanOptions", "bar", "2.dcm"),
                new TagDataFullCsvRecord("ScanOptions", "bar", "3.dcm"),
                new TagDataFullCsvRecord("ScanOptions", "foo", "1.dcm"),
                new TagDataFullCsvRecord("ScanOptions", "foo", "2.dcm"),
            };

            List<TagDataFullCsvRecord> actual = TagDataFullCsvRecord.BuildRecordList("ScanOptions", testData).ToList();

            Assert.That(actual, Is.EqualTo(expected));
        }

        #endregion
    }
}
