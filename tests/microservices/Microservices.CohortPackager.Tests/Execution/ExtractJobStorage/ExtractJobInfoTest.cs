using System;
using System.Collections.Generic;
using Microservices.CohortPackager.Execution.ExtractJobStorage;
using NUnit.Framework;
using Smi.Common.Tests;


namespace Microservices.CohortPackager.Tests.Execution.ExtractJobStorage
{
    [TestFixture]
    public class ExtractJobInfoTest
    {
        private readonly TestDateTimeProvider _dateTimeProvider = new TestDateTimeProvider();

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
                "MR",
                ExtractJobStatus.WaitingForCollectionInfo);
            var info2 = new ExtractJobInfo(
                guid,
                _dateTimeProvider.UtcNow(),
                "1234",
                "test/directory",
                "KeyTag",
                123,
                "MR",
                ExtractJobStatus.WaitingForCollectionInfo);

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
                "MR",
                ExtractJobStatus.WaitingForCollectionInfo);
            var info2 = new ExtractJobInfo(
                guid,
                _dateTimeProvider.UtcNow(),
                "1234",
                "test/directory",
                "KeyTag",
                123,
                "MR",
                ExtractJobStatus.WaitingForCollectionInfo);

            Assert.AreEqual(info1.GetHashCode(), info2.GetHashCode());
        }

        [Test]
        public void TestExtractFileCollectionInfo_Equality()
        {
            var info1 = new ExtractFileCollectionInfo(
                "Study1",
                new List<string>
                {
                    "anon1.dcm",
                    "anon2.dcm",
                }
                , new Dictionary<string, int>
                {
                    {"reject1", 1 },
                    {"reject2", 2 },
                });
            var info2 = new ExtractFileCollectionInfo(
                "Study1",
                new List<string>
                {
                    "anon1.dcm",
                    "anon2.dcm",
                }
                , new Dictionary<string, int>
                {
                    {"reject1", 1 },
                    {"reject2", 2 },
                });

            Assert.AreEqual(info1, info2);
        }

        [Test]
        public void TestExtractFileCollectionInfo_GetHashCode()
        {
            var info1 = new ExtractFileCollectionInfo(
                "Study1",
                new List<string>
                {
                    "anon1.dcm",
                    "anon2.dcm",
                }
                , new Dictionary<string, int>
                {
                    {"reject1", 1 },
                    {"reject2", 2 },
                });
            var info2 = new ExtractFileCollectionInfo(
                "Study1",
                new List<string>
                {
                    "anon1.dcm",
                    "anon2.dcm",
                }
                , new Dictionary<string, int>
                {
                    {"reject1", 1 },
                    {"reject2", 2 },
                });

            Assert.AreEqual(info1.GetHashCode(), info2.GetHashCode());
        }
        
        [Test]
        public void TestExtractFileStatusInfo_Equality()
        {
            var info1 = new ExtractFileStatusInfo("anon.dcm",true, "anonymised");
            var info2 = new ExtractFileStatusInfo("anon.dcm",true, "anonymised");

            Assert.AreEqual(info1, info2);
        }

        [Test]
        public void TestExtractFileStatusInfo_GetHashCode()
        {
            var info1 = new ExtractFileStatusInfo("anon.dcm", true, "anonymised");
            var info2 = new ExtractFileStatusInfo("anon.dcm", true, "anonymised");

            Assert.AreEqual(info1.GetHashCode(), info2.GetHashCode());
        }

        #endregion
    }
}
