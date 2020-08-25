using NUnit.Framework;
using Smi.Common.Messages;


namespace Smi.Common.Tests.Messages
{
    public class MessagingConstantsTest
    {
        #region Fixture Methods 

        [OneTimeSetUp]
        public void OneTimeSetUp() { }

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

        /// <summary>
        /// This test fails as a reminder that the associated RabbitMQ configuration needs to be updated!
        /// </summary>
        [Test]
        public void Test_Constants_NotModified()
        {
            Assert.AreEqual("verify", MessagingConstants.RMQ_EXTRACT_FILE_VERIFY_ROUTING_KEY);
            Assert.AreEqual("noverify", MessagingConstants.RMQ_EXTRACT_FILE_NOVERIFY_ROUTING_KEY);
        }

        #endregion
    }
}
