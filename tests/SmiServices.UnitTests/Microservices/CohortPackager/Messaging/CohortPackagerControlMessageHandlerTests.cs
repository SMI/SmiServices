using Moq;
using NUnit.Framework;
using SmiServices.Microservices.CohortPackager;
using SmiServices.Microservices.CohortPackager.JobProcessing;
using System;


namespace SmiServices.UnitTests.Microservices.CohortPackager.Messaging;

internal class CohortPackagerControlMessageHandlerTests
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() { }

    [SetUp]
    public void SetUp() { }

    [TearDown]
    public void TearDown() { }

    [TestCase(null)]
    [TestCase("00000000-0000-0000-0000-000000000001")]
    public void ControlMessageHandler_ProcessJobs_ValidGuids(string? jobIdStr)
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
