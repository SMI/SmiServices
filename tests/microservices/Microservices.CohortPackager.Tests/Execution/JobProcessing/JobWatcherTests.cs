
using Microservices.CohortPackager.Execution.ExtractJobStorage;
using Microservices.CohortPackager.Execution.JobProcessing;
using Moq;
using NUnit.Framework;
using Smi.Common.Options;
using Smi.Common.Tests;
using System;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;

namespace Microservices.CohortPackager.Tests.Execution.JobProcessing
{
    [TestFixture]
    public class JobWatcherTests
    {
        private GlobalOptions _globalOptions;

        private readonly List<ExtractJobInfo> _mockJobInfos = new List<ExtractJobInfo>();

        private const string TestProjectNumber = "1234-5678";
        private const string TestExtractionDirectory = @"C:\temp\extract\1234-5678\testExtract\";
        private const string ExpectedAnonFileName = "anonFile.dcm";

        #region Fixture Methods

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestLogger.Setup();

            _globalOptions = GlobalOptions.Load("default.yaml", TestContext.CurrentContext.TestDirectory);

            var jobFileCollectionInfo = new List<ExtractFileCollectionInfo>
            {
                new ExtractFileCollectionInfo("123.456.789", new List<string> {ExpectedAnonFileName})
            };

            var jobExtractFileStatuses = new List<ExtractFileStatusInfo>();

            var mockJobInfo = new ExtractJobInfo(Guid.NewGuid(), TestProjectNumber, DateTime.Now, ExtractJobStatus.WaitingForFiles, TestExtractionDirectory, 1, "SeriesInstanceUID", jobFileCollectionInfo, jobExtractFileStatuses);

            _mockJobInfos.Add(mockJobInfo);
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
        public void TestProcessJobs_Incomplete()
        {
            var mockedJobStore = Mock.Of<IExtractJobStore>(x => x.GetLatestJobInfo(It.IsAny<Guid>()) == _mockJobInfos);

            var callbackUsed = false;
            Action<Exception> exceptionCallback = exception => callbackUsed = true;

            var fileSystem = new MockFileSystem();
            fileSystem.AddDirectory(TestExtractionDirectory);

            var jobWatcher = new ExtractJobWatcher(_globalOptions.CohortPackagerOptions,
                _globalOptions.FileSystemOptions, mockedJobStore, exceptionCallback, fileSystem);

            jobWatcher.ProcessJobs();

            Assert.True(jobWatcher.JobsCompleted == 0);
            Assert.False(callbackUsed);
        }

        [Test]
        public void TestProcessJobs_Complete()
        {
            var mockedJobStore = Mock.Of<IExtractJobStore>(x => x.GetLatestJobInfo(It.IsAny<Guid>()) == _mockJobInfos);

            var callbackUsed = false;
            Action<Exception> exceptionCallback = exception => callbackUsed = true;

            var fileSystem = new MockFileSystem();
            fileSystem.AddDirectory(TestExtractionDirectory);
            fileSystem.AddFile(TestExtractionDirectory + @"\" + ExpectedAnonFileName, new MockFileData(""));

            var jobWatcher = new ExtractJobWatcher(_globalOptions.CohortPackagerOptions,
                _globalOptions.FileSystemOptions, mockedJobStore, exceptionCallback, fileSystem);

            jobWatcher.ProcessJobs();

            Assert.True(jobWatcher.JobsCompleted == 1);
            Assert.False(callbackUsed);
        }

        #endregion
    }
}
