using Microservices.CohortPackager.Execution.ExtractJobStorage;
using Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB;
using Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB.ObjectModel;
using NUnit.Framework;
using Smi.Common.Helpers;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.Tests;
using System;


namespace Microservices.CohortPackager.Tests.Execution.ExtractJobStorage.MongoDB
{
    [TestFixture]
    public class MongoExtractJobInfoExtensionsTest
    {
        private readonly DateTimeProvider _dateTimeProvider = new TestDateTimeProvider();

        private readonly MessageHeader _messageHeader = new()
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
                UserName = "testUser",
                IsIdentifiableExtraction = true,
                IsNoFilterExtraction = true,
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
                "testUser",
                "MR",
                ExtractJobStatus.WaitingForCollectionInfo,
                isIdentifiableExtraction: true,
                isNoFilterExtraction: true
                );

            Assert.That(extractJobInfo,Is.EqualTo(expected));
        }

        #endregion
    }
}
