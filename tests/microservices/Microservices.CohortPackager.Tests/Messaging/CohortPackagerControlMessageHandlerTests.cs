
using Microservices.CohortPackager.Execution.JobProcessing;
using Microservices.CohortPackager.Messaging;
using Moq;
using NUnit.Framework;
using Smi.Common.Tests;
using System;


namespace Microservices.CohortPackager.Tests.Messaging
{
    internal class CohortPackagerControlMessageHandlerTests
    {
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

        [TestCase(null)]
        [TestCase("00000000-0000-0000-0000-000000000001")]
        public void ControlMessageHandler_ProcessJobs_ValidGuids(string jobIdStr)
        {
            // Arrange

            Guid jobId = default;
            if (!string.IsNullOrWhiteSpace(jobIdStr))
                jobId = Guid.Parse(jobIdStr);

            var jobWatcherMock = new Mock<IExtractJobWatcher>(MockBehavior.Strict);
            jobWatcherMock.Setup(x => x.ProcessJobs(jobId));

            var consumer = new CohortPackagerControlMessageHandler(jobWatcherMock.Object);

            // Act

            consumer.ControlMessageHandler("processjobs", jobIdStr);

            // Assert

            jobWatcherMock.VerifyAll();
        }

        [Test]
        public void ControlMessageHandler_ProcessJobs_InvalidGuid()
        {
            // Arrange

            var jobWatcherMock = new Mock<IExtractJobWatcher>(MockBehavior.Strict);

            var consumer = new CohortPackagerControlMessageHandler(jobWatcherMock.Object);

            // Act

            consumer.ControlMessageHandler("processjobs", "not-a-guid");

            // Assert

            jobWatcherMock.VerifyAll();
        }

        [Test]
        public void ControlMessageHandler_OtherAction_Ignored()
        {
            // Arrange

            var jobWatcherMock = new Mock<IExtractJobWatcher>(MockBehavior.Strict);

            var consumer = new CohortPackagerControlMessageHandler(jobWatcherMock.Object);

            // Act

            consumer.ControlMessageHandler("something-else", "foo");

            // Assert

            jobWatcherMock.VerifyAll();
        }
    }
}
