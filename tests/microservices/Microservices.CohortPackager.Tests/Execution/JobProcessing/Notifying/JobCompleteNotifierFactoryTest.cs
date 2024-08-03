using System;
using Microservices.CohortPackager.Execution.JobProcessing.Notifying;
using NUnit.Framework;
using Smi.Common.Tests;


namespace Microservices.CohortPackager.Tests.Execution.JobProcessing.Notifying
{
    public class JobCompleteNotifierFactoryTest
    {
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
        public void GetNotifier_ConstructsLoggingNotifier()
        {
            IJobCompleteNotifier notifier = JobCompleteNotifierFactory.GetNotifier(notifierTypeStr: "LoggingNotifier");
            Assert.That(notifier is LoggingNotifier, Is.True);
        }

        [Test]
        public void GetNotifier_ThrowsException_OnInvalidNotifierTypeStr()
        {
            Assert.Throws<ArgumentException>(() => JobCompleteNotifierFactory.GetNotifier(notifierTypeStr: "foo"));
        }

        #endregion
    }
}
