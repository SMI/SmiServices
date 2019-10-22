
using NUnit.Framework;
using Smi.Common.Tests;


namespace Microservices.MongoDBPopulator.Tests.Messaging
{
    [TestFixture, RequiresMongoDb]
    public class MongoDbPopulatorMessageConsumerTests
    {
        private MongoDbPopulatorTestHelper _helper;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _helper = new MongoDbPopulatorTestHelper();
            _helper.SetupSuite();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _helper.Dispose();
        }

        /// <summary>
        /// Test a variety of invalid messages to ensure they are NACK'd
        /// </summary>
        [Test]
        public void TestInvalidMessagesNack()
        {
            // Empty message
            // Invalid uids etc.
            // Empty or null dataset

            Assert.Inconclusive("Not implemented");
        }
    }
}
