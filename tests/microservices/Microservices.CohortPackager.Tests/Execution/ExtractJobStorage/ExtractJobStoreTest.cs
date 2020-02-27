using System;
using System.Collections.Generic;
using Microservices.CohortPackager.Execution.ExtractJobStorage;
using Moq;
using NUnit.Framework;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.Tests;


namespace Microservices.CohortPackager.Tests.Execution.ExtractJobStorage
{
    [TestFixture]
    public class ExtractJobStoreTest
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

        private class MockExtractJobStore : ExtractJobStore
        {
            protected override void PersistMessageToStoreImpl(ExtractionRequestInfoMessage message, IMessageHeader header) { }
            protected override void PersistMessageToStoreImpl(ExtractFileCollectionInfoMessage collectionInfoMessage, IMessageHeader header) { }
            protected override void PersistMessageToStoreImpl(ExtractFileStatusMessage message, IMessageHeader header) { }
            protected override void PersistMessageToStoreImpl(IsIdentifiableMessage message, IMessageHeader header) { }
            protected override List<ExtractJobInfo> GetLatestJobInfoImpl(Guid jobId = new Guid()) => throw new NotImplementedException();
            protected override void CleanupJobDataImpl(Guid jobId) { }
            protected override void QuarantineJobImpl(Guid jobId, Exception e) { }
        }

        #endregion

        #region Test Methods

        [Test]
        public void TestPersistMessageToStore_ExtractionRequestInfoMessage()
        {
            var mockJobStore = new MockExtractJobStore();
            var testMessage = new ExtractionRequestInfoMessage();
            var mockHeader = new Mock<IMessageHeader>();

            // TODO(rkm 2020-02-27) Use the ExtractionKey enum
            testMessage.KeyTag = "SeriesInstanceUID";
            testMessage.ExtractionModality = null;
            mockJobStore.PersistMessageToStore(testMessage, mockHeader.Object);

            testMessage.KeyTag = "StudyInstanceUID";
            testMessage.ExtractionModality = "MR";
            mockJobStore.PersistMessageToStore(testMessage, mockHeader.Object);

            testMessage.KeyTag = "StudyInstanceUID";
            testMessage.ExtractionModality = null;
            Assert.Throws<ApplicationException>(() => mockJobStore.PersistMessageToStore(testMessage, mockHeader.Object));

            testMessage.KeyTag = "SeriesInstanceUID";
            testMessage.ExtractionModality = "MR";
            Assert.Throws<ApplicationException>(() => mockJobStore.PersistMessageToStore(testMessage, mockHeader.Object));
        }

        [Test]
        public void TestPersistMessageToStore_ExtractFileStatusMessage()
        {
            var mockJobStore = new MockExtractJobStore();
            var testMessage = new ExtractFileStatusMessage();
            var mockHeader = new Mock<IMessageHeader>();

            // TODO(rkm 2020-02-27) Use the ExtractionKey enum
            testMessage.Status = ExtractFileStatus.Unknown;
            mockJobStore.PersistMessageToStore(testMessage, mockHeader.Object);

            testMessage.Status = ExtractFileStatus.Anonymised;
            Assert.Throws<ApplicationException>(() => mockJobStore.PersistMessageToStore(testMessage, mockHeader.Object));
        }

        #endregion
    }
}
