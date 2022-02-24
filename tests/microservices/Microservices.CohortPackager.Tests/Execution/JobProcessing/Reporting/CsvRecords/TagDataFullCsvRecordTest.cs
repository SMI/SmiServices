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

        [TestCase("tagName", null, "foo", 0, FailureClassification.Person, "foo")]
        [TestCase("tagName", "", "foo", 0, FailureClassification.Person, "foo")]
        [TestCase("failureValue", "foo", null, 0, FailureClassification.Person, "foo")]
        [TestCase("failureValue", "foo", "", 0, FailureClassification.Person, "foo")]
        [TestCase("offset", "foo", "foo", -123, FailureClassification.Person, "foo")]
        [TestCase("failureClassification", "foo", "foo", 0, FailureClassification.None, "foo")]
        [TestCase("filePath", "foo", "foo", 0, FailureClassification.Person, null)]
        [TestCase("filePath", "foo", "foo", 0, FailureClassification.Person, "")]
        public void Constructor_ThrowsArgumentException_OnInvalidArgs(
            string expected,
            string tagName,
            string failureValue,
            int offset,
            FailureClassification classification,
            string filePath
        )
        {
            var exc = Assert.Throws<ArgumentException>(() => { var _ = new TagDataFullCsvRecord(tagName, failureValue, offset, classification, filePath); });
            Assert.AreEqual(expected, exc.Message);
        }

        [Test]
        public void BuildRecordList_Empty()
        {
            IEnumerable<TagDataFullCsvRecord> records = TagDataFullCsvRecord.BuildRecordList("foo", new List<Tuple<string, FailureData>>());
            Assert.AreEqual(Enumerable.Empty<TagDataFullCsvRecord>(), records);
        }

        [Test]
        public void BuildRecordList_WithData()
        {
            // Arrange

            var testData = new List<Tuple<string, FailureData>>()
            {
                new Tuple<string, FailureData>(
                    "foo.dcm",
                    new FailureData(
                        parts: new List<FailurePart>()
                        {
                            new FailurePart("Bar", FailureClassification.Person, 7),
                            new FailurePart("Foo", FailureClassification.Person, 3),
                        },
                        problemField: "unused",
                        problemValue: "unused"
                    )
                ),
                   new Tuple<string, FailureData>(
                    "bar.dcm",
                    new FailureData(
                        parts: new List<FailurePart>()
                        {
                            new FailurePart("Foo", FailureClassification.Person, 0),
                        },
                        problemField: "unused",
                        problemValue: "unused"
                    )
                ),
                new Tuple<string, FailureData>(
                    "baz.dcm",
                    new FailureData(
                        parts: new List<FailurePart>()
                        {
                            new FailurePart("Foo", FailureClassification.Person, 3),
                            new FailurePart("Bar", FailureClassification.Person, 7),
                            new FailurePart("1970-01-01", FailureClassification.Date, 0),
                        },
                        problemField: "unused",
                        problemValue: "unused"
                    )
                )
            };

            var expected = new List<TagDataFullCsvRecord>
            {
                new TagDataFullCsvRecord("Tag", "1970-01-01", 0, FailureClassification.Date,  "baz.dcm"),
                new TagDataFullCsvRecord("Tag", "Foo", 3, FailureClassification.Person,  "baz.dcm"),
                new TagDataFullCsvRecord("Tag", "Bar", 7, FailureClassification.Person,  "baz.dcm"),
                new TagDataFullCsvRecord("Tag", "Foo", 3, FailureClassification.Person,  "foo.dcm"),
                new TagDataFullCsvRecord("Tag", "Bar", 7, FailureClassification.Person,  "foo.dcm"),
                new TagDataFullCsvRecord("Tag", "Foo", 0, FailureClassification.Person,  "bar.dcm"),
            };

            // Act

            List<TagDataFullCsvRecord> actual = TagDataFullCsvRecord.BuildRecordList("Tag", testData).ToList();

            // Assert

            Assert.AreEqual(expected, actual);
        }

        #endregion
    }
}
