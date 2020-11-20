using Microservices.CohortPackager.Execution.JobProcessing.Reporting.CsvRecords;
using NUnit.Framework;
using Smi.Common.Tests;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Microservices.CohortPackager.Tests.Execution.JobProcessing.Reporting.CsvRecords
{
    public class TagDataFrequencyRecordTest
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
            Assert.Throws<ArgumentException>(() => { var _ = new TagDataFrequencyRecord(0, 0, -0.1); });
        }

        [Test]
        public void BuildRecordList_Empty()
        {
            IEnumerable<TagDataFrequencyRecord> records = TagDataFrequencyRecord.BuildRecordList(new Dictionary<uint, uint>());
            Assert.AreEqual(Enumerable.Empty<TagDataFrequencyRecord>(), records);
        }

        [Test]
        public void BuildRecordList_WithData()
        {
            var testData = new Dictionary<uint, uint>
            {
                {4, 8},
                {1, 2},
                {2, 4},
            };

            var expected = new List<TagDataFrequencyRecord>
            {
                new TagDataFrequencyRecord(1, 2, 1.0 * 2 / 14),
                new TagDataFrequencyRecord(2, 4, 1.0 * 4 / 14),
                new TagDataFrequencyRecord(3, 0, 0.0),
                new TagDataFrequencyRecord(4, 8, 1.0 * 8 / 14),
            };

            List<TagDataFrequencyRecord> actual = TagDataFrequencyRecord.BuildRecordList(testData).ToList();

            Assert.AreEqual(expected, actual);
        }

        #endregion
    }
}
