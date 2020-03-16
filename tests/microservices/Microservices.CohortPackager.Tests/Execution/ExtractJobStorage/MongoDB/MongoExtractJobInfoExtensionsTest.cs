using System;
using System.Collections.Generic;
using Microservices.CohortPackager.Execution.ExtractJobStorage;
using Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB;
using Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB.ObjectModel;
using NUnit.Framework;
using Smi.Common.Helpers;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.Tests;


namespace Microservices.CohortPackager.Tests.Execution.ExtractJobStorage.MongoDB
{
    [TestFixture]
    public class MongoExtractJobInfoExtensionsTest
    {
        private readonly DateTimeProvider _dateTimeProvider = new TestDateTimeProvider();

        private readonly MessageHeader _messageHeader = new MessageHeader
        {
            Parents = new[] { Guid.NewGuid(), },
        };

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
        public void TestToExtractJobInfo()
        {
            Guid guid = Guid.NewGuid();
            var message = new ExtractionRequestInfoMessage
            {
                ExtractionModality = "MR",
                JobSubmittedAt = _dateTimeProvider.UtcNow(),
                ProjectNumber = "1234",
                ExtractionJobIdentifier = guid,
                ExtractionDirectory = "test/directory",
                KeyTag = "KeyTag",
                KeyValueCount = 123,
            };

            MongoExtractJobDoc doc = MongoExtractJobDoc.FromMessage(message, _messageHeader, _dateTimeProvider);
            ExtractJobInfo extractJobInfo = doc.ToExtractJobInfo();

            var expected = new ExtractJobInfo(
                guid,
                _dateTimeProvider.UtcNow(),
                "1234",
                "test/directory",
                "KeyTag",
                123,
                "MR",
                ExtractJobStatus.WaitingForCollectionInfo);

            Assert.AreEqual(expected, extractJobInfo);
        }

        [Test]
        public void TestToExtractFIleCollectionInfo()
        {
            var expectedFilesDoc = new MongoExpectedFilesDoc(
                MongoExtractionMessageHeaderDoc.FromMessageHeader(Guid.NewGuid(), _messageHeader, _dateTimeProvider),
                "KeyTag",
                new HashSet<MongoExpectedFileInfoDoc>
                {
                    new MongoExpectedFileInfoDoc(Guid.NewGuid(), "anon1.dcm"),
                    new MongoExpectedFileInfoDoc(Guid.NewGuid(), "anon2.dcm"),
                },
                new MongoRejectedKeyInfoDoc(
                    MongoExtractionMessageHeaderDoc.FromMessageHeader(Guid.NewGuid(), _messageHeader, _dateTimeProvider),
                    new Dictionary<string, int>
                    {
                        {"reject1", 1},
                        {"reject2", 2},
                    })
                );
            ExtractFileCollectionInfo collectionInfo = expectedFilesDoc.ToExtractFileCollectionInfo();

            var expected = new ExtractFileCollectionInfo(
                "KeyTag",
                new List<string>
                {
                    "anon1.dcm",
                    "anon2.dcm",
                },
                new Dictionary<string, int>
                {
                    {"reject1", 1},
                    {"reject2", 2},
                }
            );

            Assert.AreEqual(expected, collectionInfo);
        }

        [Test]
        public void TestToExtractFileStatusInfo()
        {
            var statusDoc = new MongoFileStatusDoc(
                MongoExtractionMessageHeaderDoc.FromMessageHeader(Guid.NewGuid(), _messageHeader, _dateTimeProvider),
                "anon.dcm",
                true,
                false,
                "anonymised");
            ExtractFileStatusInfo statusInfo = statusDoc.ToExtractFileStatusInfo();

            var expected = new ExtractFileStatusInfo(
                "anon.dcm",
                false,
                "anonymised");

            Assert.AreEqual(expected, statusInfo);
        }

        #endregion
    }
}
