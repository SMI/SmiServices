using Microservices.CohortPackager.Execution.JobProcessing.Reporting.CsvRecords;
using NUnit.Framework;
using Smi.Common.Tests;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Microservices.CohortPackager.Tests.Execution.JobProcessing.Reporting.CsvRecords
{
    public class TagDataSummaryCsvRecordTest
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
        [TestCase(null, "foo", 1U, 1.0)]
        [TestCase("  ", "foo", 1U, 1.0)]
        // failureValue
        [TestCase("foo", null, 1U, 1.0)]
        [TestCase("foo", "  ", 1U, 1.0)]
        // occurrences
        [TestCase("foo", "foo", 0U, 1.0)]
        // frequency
        [TestCase("foo", "foo", 1U, 0.0)]
        [TestCase("foo", "foo", 1U, -1.0)]
        public void Constructor_ThrowsArgumentException_OnInvalidArgs(string? tagName, string? failureValue, uint occurrences, double frequency)
        {
            Assert.Throws<ArgumentException>(() => { var _ = new TagDataSummaryCsvRecord(tagName, failureValue, occurrences, frequency); });
        }

        [Test]
        public void RelativeFrequencyInReport_OnlySetOnce()
        {
            var record = new TagDataSummaryCsvRecord("ScanOptions", "foo", 1, 1);
            record.RelativeFrequencyInReport = 1;
            Assert.Throws<ArgumentException>(() => record.RelativeFrequencyInReport = 1);
        }

        [Test]
        public void BuildRecordList_Empty()
        {
            IEnumerable<TagDataSummaryCsvRecord> records = TagDataSummaryCsvRecord.BuildRecordList("foo", new Dictionary<string, List<string>>());
            Assert.That(records, Is.EqualTo(Enumerable.Empty<TagDataSummaryCsvRecord>()));
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
                        "4.dcm",
                    }
                },
            };

            var expected = new List<TagDataSummaryCsvRecord>
            {
                new TagDataSummaryCsvRecord("ScanOptions", "bar", 4, 1.0 * 4 / 6),
                new TagDataSummaryCsvRecord("ScanOptions", "foo", 2, 1.0 * 2 / 6),
            };

            List<TagDataSummaryCsvRecord> actual = TagDataSummaryCsvRecord.BuildRecordList("ScanOptions", testData).ToList();

            Assert.That(actual, Is.EqualTo(expected));
        }

        #endregion
    }
}
