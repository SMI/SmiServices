using Microservices.CohortPackager.Execution.JobProcessing.Reporting;
using Microservices.CohortPackager.Execution.JobProcessing.Reporting.CsvRecords;
using Microservices.IsIdentifiable.Failures;
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

        [TestCase("tagName", null, "foo", 123, FailureClassification.Person, 1U, 1.0)]
        [TestCase("tagName", "  ", "foo", 123, FailureClassification.Person, 1U, 1.0)]
        [TestCase("failureValue", "foo", null, 123, FailureClassification.Person, 1U, 1.0)]
        [TestCase("failureValue", "foo", "  ", 123, FailureClassification.Person, 1U, 1.0)]
        [TestCase("offset", "foo", "foo", -2, FailureClassification.Person, 1U, 1.0)]
        [TestCase("failureClassification", "foo", "foo", 123, FailureClassification.None, 1U, 1.0)]
        [TestCase("occurrences", "foo", "foo", 123, FailureClassification.Person, 0U, 1.0)]
        [TestCase("frequency", "foo", "foo", 123, FailureClassification.Person, 1U, 0.0)]
        [TestCase("frequency", "foo", "foo", 123, FailureClassification.Person, 1U, -1.0)]
        public void Constructor_ThrowsArgumentException_OnInvalidArgs(
            string expected,
            string tagName,
            string failureValue,
            int offset,
            FailureClassification classification,
            uint occurrences,
            double frequency
        )
        {
            var exc = Assert.Throws<ArgumentException>(() => { var _ = new TagDataSummaryCsvRecord(tagName, failureValue, offset, classification, occurrences, frequency); });
            Assert.AreEqual(expected, exc.Message);
        }

        [Test]
        public void RelativeFrequencyInReport_OnlySetOnce()
        {
            var record = new TagDataSummaryCsvRecord("ScanOptions", "foo", 0, FailureClassification.Location, 1, 1);
            record.RelativeFrequencyInReport = 1;
            Assert.Throws<ArgumentException>(() => record.RelativeFrequencyInReport = 1);
        }

        [Test]
        public void BuildRecordList_Empty()
        {
            IEnumerable<TagDataSummaryCsvRecord> records = TagDataSummaryCsvRecord.BuildRecordList("foo", new List<FailureData>());
            Assert.AreEqual(Enumerable.Empty<TagDataSummaryCsvRecord>(), records);
        }

        [Test]
        public void BuildRecordList_HappyPath()
        {
            // Arrange

            var testData = new List<FailureData>()
            {
                new FailureData(
                    parts: new List<FailurePart>()
                    {
                        new FailurePart("foo", FailureClassification.Person, 0),
                    },
                    problemField: "unused",
                    problemValue: "foo"
                ),
            };

            var expected = new List<TagDataSummaryCsvRecord>
            {
                new TagDataSummaryCsvRecord("ScanOptions", "foo", 0, FailureClassification.Person, 1, 1.0),
            };

            // Act

            List<TagDataSummaryCsvRecord> actual = TagDataSummaryCsvRecord.BuildRecordList("ScanOptions", testData).ToList();

            // Assert

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void BuildRecordList_Complex()
        {
            // Arrange

            var testData = new List<FailureData>()
            {
                new FailureData(
                    parts: new List<FailurePart>()
                    {
                        new FailurePart("Foo", FailureClassification.Person, 3),
                        new FailurePart("Bar", FailureClassification.Person, 7),
                    },
                    problemField: "unused",
                    problemValue: "Dr Foo Bar"
                ),
                new FailureData(
                    parts: new List<FailurePart>()
                    {
                        new FailurePart("Foo", FailureClassification.Person, 3),
                        new FailurePart("Bar", FailureClassification.Person, 7),
                    },
                    problemField: "unused",
                    problemValue: "Dr Foo Bar"
                ),
                new FailureData(
                    parts: new List<FailurePart>()
                    {
                        new FailurePart("Org", FailureClassification.Organization, 0),
                    },
                    problemField: "unused",
                    problemValue: "Org"
                )
            };

            var expected = new List<TagDataSummaryCsvRecord>
            {
                new TagDataSummaryCsvRecord("Tag", "Foo", 3, FailureClassification.Person, 2, 0.4),
                new TagDataSummaryCsvRecord("Tag", "Bar", 7, FailureClassification.Person, 2, 0.4),
                new TagDataSummaryCsvRecord("Tag",  "Org", 0, FailureClassification.Organization, 1, 0.2),
            };

            // Act

            List<TagDataSummaryCsvRecord> actual = TagDataSummaryCsvRecord.BuildRecordList("Tag", testData).ToList();

            // Assert

            Assert.AreEqual(expected, actual);
        }

        #endregion
    }
}
