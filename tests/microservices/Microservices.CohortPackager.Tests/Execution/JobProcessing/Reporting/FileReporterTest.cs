using Microservices.CohortPackager.Execution.ExtractJobStorage;
using Microservices.CohortPackager.Execution.JobProcessing.Reporting;
using Moq;
using NUnit.Framework;
using Smi.Common.Tests;
using System;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;


namespace Microservices.CohortPackager.Tests.Execution.JobProcessing.Reporting
{
    public class FileReporterTest
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
        public void CreateReport_WritesJobIdToFile()
        {
            var jobId = Guid.NewGuid();
            const string extractionRoot = "root";
            const string extractionDir = "proj1/extract";

            var mockJobStore = new Mock<IExtractJobStore>(MockBehavior.Strict);
            mockJobStore
                .Setup(
                    x => x.GetCompletedJobInfo(It.IsAny<Guid>())
                )
                .Returns(
                    new CompletedExtractJobInfo(
                        jobId,
                        DateTime.UtcNow,
                        DateTime.UtcNow + TimeSpan.FromHours(1),
                        "1234",
                        extractionDir,
                        "SeriesInstanceUID",
                        1,
                        null,
                        false,
                        false
                    )
                );
            mockJobStore.Setup(x => x.GetCompletedJobRejections(It.IsAny<Guid>())).Returns(Enumerable.Empty<ExtractionIdentifierRejectionInfo>());
            mockJobStore.Setup(x => x.GetCompletedJobAnonymisationFailures(It.IsAny<Guid>())).Returns(Enumerable.Empty<FileAnonFailureInfo>());
            mockJobStore.Setup(x => x.GetCompletedJobVerificationFailures(It.IsAny<Guid>())).Returns(Enumerable.Empty<FileVerificationFailureInfo>());

            var mockFileSystem = new MockFileSystem();
            mockFileSystem.Directory.CreateDirectory(mockFileSystem.Path.Combine(extractionRoot, extractionDir));

            var reporter = new FileReporter(mockJobStore.Object, mockFileSystem, extractionRoot, ReportFormat.Split, reportNewLine: null);

            reporter.CreateReport(jobId);

            string expectedJobIdFile = mockFileSystem.Path.Combine(extractionRoot, extractionDir, "jobId.txt");
            Assert.True(mockFileSystem.FileExists(expectedJobIdFile));
            Assert.AreEqual(jobId.ToString(), mockFileSystem.File.ReadAllLines(expectedJobIdFile)[0]);
        }

        #endregion
    }
}
