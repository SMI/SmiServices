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

        [Test]
        public void Constructor_ThrowsArgumentException_OnInvalidArgs()
        {
            Assert.Throws<ArgumentException>(() => { var _ = new TagDataSummaryCsvRecord(null, "foo", 1, 1.0); });
            Assert.Throws<ArgumentException>(() => { var _ = new TagDataSummaryCsvRecord("", "foo", 1, 1.0); });

            Assert.Throws<ArgumentException>(() => { var _ = new TagDataSummaryCsvRecord("foo", null, 1, 1.0); });
            Assert.Throws<ArgumentException>(() => { var _ = new TagDataSummaryCsvRecord("foo", "", 1, 1.0); });

            Assert.Throws<ArgumentException>(() => { var _ = new TagDataSummaryCsvRecord("foo", "foo", 0, 1.0); });

            Assert.Throws<ArgumentException>(() => { var _ = new TagDataSummaryCsvRecord("foo", "foo", 1, 0.0); });
            Assert.Throws<ArgumentException>(() => { var _ = new TagDataSummaryCsvRecord("foo", "foo", 1, -1.0); });
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
            Assert.AreEqual(Enumerable.Empty<TagDataSummaryCsvRecord>(), records);
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

            Assert.AreEqual(expected, actual);
        }

        #endregion
    }
}
