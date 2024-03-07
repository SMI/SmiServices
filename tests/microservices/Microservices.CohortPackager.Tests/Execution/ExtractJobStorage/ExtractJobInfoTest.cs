using Microservices.CohortPackager.Execution.ExtractJobStorage;
using NUnit.Framework;
using Smi.Common.Tests;
using System;

namespace Microservices.CohortPackager.Tests.Execution.ExtractJobStorage
{
    [TestFixture]
    public class ExtractJobInfoTest
    {
        private readonly TestDateTimeProvider _dateTimeProvider = new();

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

        [TestCase("proj/foo/extract-name")]
        [TestCase("proj\\foo\\extract-name")]
        public void Test_ExtractJobInfo_ExtractionName(string extractionDir)
        {
            var info = new ExtractJobInfo(
                Guid.NewGuid(),
                DateTime.UtcNow,
                "1234",
                extractionDir,
                "KeyTag",
                123,
                "testUser",
                "MR",
                ExtractJobStatus.WaitingForCollectionInfo,
                isIdentifiableExtraction: false,
                isNoFilterExtraction: false
            );

            Assert.AreEqual("extract-name", info.ExtractionName());
        }

        [TestCase("proj/foo/extract-name", "proj/foo")]
        [TestCase("proj\\foo\\extract-name", "proj\\foo")]
        public void Test_ExtractJobInfo_ProjectExtractionDir(string extractionDir, string expected)
        {
            var info = new ExtractJobInfo(
                Guid.NewGuid(),
                DateTime.UtcNow,
                "1234",
                extractionDir,
                "KeyTag",
                123,
                "testUser",
                "MR",
                ExtractJobStatus.WaitingForCollectionInfo,
                isIdentifiableExtraction: false,
                isNoFilterExtraction: false
            );

            Assert.AreEqual(expected, info.ProjectExtractionDir());
        }


        [Test]
        public void TestExtractJobInfo_Equality()
        {
            Guid guid = Guid.NewGuid();
            var info1 = new ExtractJobInfo(
                guid,
                _dateTimeProvider.UtcNow(),
                "1234",
                "test/directory",
                "KeyTag",
                123,
                "testUser",
                "MR",
                ExtractJobStatus.WaitingForCollectionInfo,
                isIdentifiableExtraction: true,
                isNoFilterExtraction: true
                );
            var info2 = new ExtractJobInfo(
                guid,
                _dateTimeProvider.UtcNow(),
                "1234",
                "test/directory",
                "KeyTag",
                123,
                "testUser",
                "MR",
                ExtractJobStatus.WaitingForCollectionInfo,
                isIdentifiableExtraction: true,
                isNoFilterExtraction: true
                );

            Assert.AreEqual(info1, info2);
        }

        [Test]
        public void TestExtractJobInfo_GetHashCode()
        {
            Guid guid = Guid.NewGuid();
            var info1 = new ExtractJobInfo(
                guid,
                _dateTimeProvider.UtcNow(),
                "1234",
                "test/directory",
                "KeyTag",
                123,
                "testUser",
                "MR",
                ExtractJobStatus.WaitingForCollectionInfo,
                isIdentifiableExtraction: true,
                isNoFilterExtraction: true
                );
            var info2 = new ExtractJobInfo(
                guid,
                _dateTimeProvider.UtcNow(),
                "1234",
                "test/directory",
                "KeyTag",
                123,
                "testUser",
                "MR",
                ExtractJobStatus.WaitingForCollectionInfo,
                isIdentifiableExtraction: true,
                isNoFilterExtraction: true
                );

            Assert.AreEqual(info1.GetHashCode(), info2.GetHashCode());
        }

        #endregion
    }
}
