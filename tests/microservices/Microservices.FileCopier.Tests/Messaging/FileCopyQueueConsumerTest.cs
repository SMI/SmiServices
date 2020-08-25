using NUnit.Framework;

namespace Microservices.FileCopier.Tests.Messaging
{
    public class FileCopyQueueConsumerTest
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

        [Test]
        public void Test_FileCopyQueueConsumer_ValidMessage_IsAcked()
        {
            // There's a ridiculous amount of boilerplate required to test this at the moment...
            //var mockFileCopier = new Mock<IFileCopier>(MockBehavior.Strict);
            //var consumer = new FileCopyQueueConsumer(mockFileCopier.Object);
            //consumer.ProcessMessage();
            
            // TODO(rkm 2020-08-25) Test Ack / not Nack
            Assert.Inconclusive();
        }

        [Test]
        public void Test_FileCopyQueueConsumer_ApplicationException_IsNacked()
        {
            // TODO(rkm 2020-08-25) Test Nack / not Ack
            Assert.Inconclusive();
        }

        [Test]
        public void Test_FileCopyQueueConsumer_UnknownException_CallsFatalCallback()
        {
            // TODO(rkm 2020-08-25) Test not ack / not nack / Fatal called
            Assert.Inconclusive();
        }

        #endregion
    }
}
