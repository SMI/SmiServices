
using Microservices.CohortPackager.Execution.JobProcessing;
using Microservices.CohortPackager.Messaging;
using Moq;
using NUnit.Framework;
using Smi.Common.Tests;
using System;


namespace Microservices.CohortPackager.Tests.Messaging
{
    public class CohortPackagerControlMessageHandlerTests
    {
        [Test]
        [TestCase(null)]
        [TestCase("00000000-0000-0000-0000-000000000001")]
        public void TestControlMessagesCausesProcess(string message)
        {
            TestLogger.Setup();

            var mockedWatcher = new Mock<IExtractJobWatcher>();

            Guid parsed = default(Guid);
            if (!string.IsNullOrWhiteSpace(message))
                parsed = Guid.Parse(message);

            var consumer = new CohortPackagerControlMessageHandler(mockedWatcher.Object);
            consumer.ControlMessageHandler("processjobs", message);

            mockedWatcher.Verify(x => x.ProcessJobs(parsed));
        }
    }
}
