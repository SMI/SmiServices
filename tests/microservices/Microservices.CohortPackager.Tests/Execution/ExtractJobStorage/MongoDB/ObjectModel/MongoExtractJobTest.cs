using System;
using Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB.ObjectModel;
using NUnit.Framework;
using Smi.Common.Messages;
using Smi.Common.Tests;


namespace Microservices.CohortPackager.Tests.Execution.ExtractJobStorage.MongoDB.ObjectModel
{
    [TestFixture]
    public class ExtractJobHeaderTest
    {
        #region Fixture Methods 

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestLogger.Setup();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() { }

        [SetUp]
        public void SetUp() { }

        [TearDown]
        public void TearDown() { }

        #endregion

        #region Tests

        [Test]
        public void TestFromMessageHeader()
        {
            Guid messageGuid = Guid.NewGuid();
            var header = new MessageHeader
            {
                MessageGuid = messageGuid,
                ProducerExecutableName = "TestFromMessageHeader",
                ProducerProcessID = 1234,
                Parents = null,
                OriginalPublishTimestamp = default,
            };
            var dateTimeProvider = new TestDateTimeProvider();

            MongoExtractJobHeader mongoExtractJobHeader = MongoExtractJobHeader.FromMessageHeader(header, dateTimeProvider);

            var expected = new MongoExtractJobHeader
            {
                ReceivedAt = dateTimeProvider.UtcNow(),
                ExtractRequestInfoMessageGuid = messageGuid,
                ProducerIdentifier = "TestFromMessageHeader(1234)",
            };
            Assert.AreEqual(expected, mongoExtractJobHeader);
        }

        #endregion
    }
}
