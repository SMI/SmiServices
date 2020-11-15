using Microservices.IsIdentifiable.Reporting.Destinations;
using Microservices.IsIdentifiable.Reporting.Reports;
using Moq;
using NUnit.Framework;
using Smi.Common.Tests;
using System.Data;
using System.Drawing.Imaging;
using System.IO.Abstractions;


namespace Microservices.IsIdentifiable.Tests.Reporting.Reports
{
    public class PixelTextFailureReportTest
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

        private class TestDataTableDestination : IReportDestination
        {
            public DataTable Dt = new DataTable();

            public void WriteItems(DataTable items) => Dt = items;
            public void Dispose() { }
            public void WriteHeader(params string[] headers) { }
        }

        [Test]
        public void IgnoreTextLengthsLessThan()
        {
            var destination = new TestDataTableDestination();
            var report = new PixelTextFailureReport("foo", ignoreTextLessThan: 4);
            report.Destinations.Add(destination);

            var mockFileInfo = new Mock<IFileInfo>(MockBehavior.Strict);
            mockFileInfo.Setup(x => x.FullName).Returns("foo");

            report.FoundPixelData(mockFileInfo.Object, "sop", PixelFormat.Alpha, PixelFormat.Alpha, "study", "series", "modality", new[] { "" }, 0, textLength: 1, "ignored", 0);
            report.FoundPixelData(mockFileInfo.Object, "sop", PixelFormat.Alpha, PixelFormat.Alpha, "study", "series", "modality", new[] { "" }, 0, textLength: 4, "reported", 0);
            report.CloseReport();

            Assert.AreEqual(1, destination.Dt.Rows.Count);
            Assert.AreEqual("reported", destination.Dt.Rows[0][12]);
        }

        #endregion
    }
}
