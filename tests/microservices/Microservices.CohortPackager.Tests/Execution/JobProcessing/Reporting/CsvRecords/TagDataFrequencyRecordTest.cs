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

        [TestCase(0U, 0U, -0.1)]
        public void Constructor_ThrowsArgumentException_OnInvalidArgs(uint wordLength, uint count, double relativeFrequencyInReport)
        {
            Assert.Throws<ArgumentException>(() => { var _ = new TagDataFrequencyRecord(wordLength, count, relativeFrequencyInReport); });
        }

        [Test]
        public void BuildRecordList_Empty()
        {
            IEnumerable<TagDataFrequencyRecord> records = TagDataFrequencyRecord.BuildRecordList(new Dictionary<uint, uint>());
            Assert.That(records, Is.EqualTo(Enumerable.Empty<TagDataFrequencyRecord>()));
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

            Assert.That(actual, Is.EqualTo(expected));
        }

        #endregion
    }
}
