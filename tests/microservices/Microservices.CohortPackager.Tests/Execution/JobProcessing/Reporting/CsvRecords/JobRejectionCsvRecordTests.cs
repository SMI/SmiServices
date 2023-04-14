using Microservices.CohortPackager.Execution.ExtractJobStorage;
using Microservices.CohortPackager.Execution.JobProcessing.Reporting.CsvRecords;
using NUnit.Framework;
using Smi.Common.Tests;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Microservices.CohortPackager.Tests.Execution.JobProcessing.Reporting.CsvRecords
{
    public class JobRejectionCsvRecordTests
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

        [TestCase(null, "foo", 123, "requestedUid")]
        [TestCase("  ", "foo", 123, "requestedUid")]
        [TestCase("1.2.3.4", null, 123, "reason")]
        [TestCase("1.2.3.4", "   ", 123, "reason")]
        [TestCase("1.2.3.4", "foo", 0, "count")]
        public void Constructor_ThrowsArgumentException_OnInvalidArgs(string requestedUid, string reason, int count, string expected)
        {
            var exc = Assert.Throws<ArgumentException>(() => new JobRejectionCsvRecord(requestedUid, reason, (uint)count));
            Assert.True(exc.Message.Contains(expected));
        }

        [Test]
        public void FromExtractionIdentifierRejectionInfos_Empty()
        {
            var _ = JobRejectionCsvRecord.FromExtractionIdentifierRejectionInfos(new List<ExtractionIdentifierRejectionInfo>());
        }

        [Test]
        public void FromExtractionIdentifierRejectionInfos_Basic()
        {
            // Arrange
            var rejectionInfos = new List<ExtractionIdentifierRejectionInfo>
        {
            new ExtractionIdentifierRejectionInfo(
                "foo",
                new Dictionary<string, int>()
                {
                    {"a", 123 },
                    {"b", 456 },
                }
            ),
            new ExtractionIdentifierRejectionInfo(
                "bar",
                new Dictionary<string, int>()
                {
                    {"a", 1 },
                }
            )
        };

            var expected = new List<JobRejectionCsvRecord>
        {
            new JobRejectionCsvRecord("foo", "a", 123),
            new JobRejectionCsvRecord("foo", "b", 456),
            new JobRejectionCsvRecord("bar", "a", 1),
        };

            // Act
            var records = JobRejectionCsvRecord.FromExtractionIdentifierRejectionInfos(rejectionInfos).ToList();

            // Assert
            Assert.AreEqual(expected, records);
        }

        #endregion
    }
}
