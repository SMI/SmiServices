using System;
using System.Collections.Generic;
using Microservices.CohortPackager.Execution.ExtractJobStorage;
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

        private class TestExtractJobStore : ExtractJobStore
        {
            protected override void PersistMessageToStoreImpl(ExtractionRequestInfoMessage message, IMessageHeader header) { }
            protected override void PersistMessageToStoreImpl(ExtractFileCollectionInfoMessage collectionInfoMessage, IMessageHeader header) => throw new NotImplementedException();
            protected override void PersistMessageToStoreImpl(ExtractFileStatusMessage message, IMessageHeader header) { }
            protected override void PersistMessageToStoreImpl(IsIdentifiableMessage message, IMessageHeader header) { }
            protected override List<ExtractJobInfo> GetReadyJobsImpl(Guid specificJobId = new Guid()) => throw new NotImplementedException();
            protected override void CompleteJobImpl(Guid jobId) => throw new NotImplementedException();
            protected override void MarkJobFailedImpl(Guid jobId, Exception e) => throw new NotImplementedException();
        }

        #endregion

        #region Test Methods

        [Test]
        public void TestPersistMessageToStore_ExtractionRequestInfoMessage()
        {
            var testExtractJobStore = new TestExtractJobStore();
            var message = new ExtractionRequestInfoMessage();
            var mockHeader = new MessageHeader();

            message.KeyTag = "SeriesInstanceUID";
            message.ExtractionModality = null;
            testExtractJobStore.PersistMessageToStore(message, mockHeader);

            message.KeyTag = "StudyInstanceUID";
            message.ExtractionModality = "MR";
            testExtractJobStore.PersistMessageToStore(message, mockHeader);

            message.KeyTag = "StudyInstanceUID";
            message.ExtractionModality = null;
            Assert.Throws<ApplicationException>(() => testExtractJobStore.PersistMessageToStore(message, mockHeader));

            message.KeyTag = "SeriesInstanceUID";
            message.ExtractionModality = "MR";
            Assert.Throws<ApplicationException>(() => testExtractJobStore.PersistMessageToStore(message, mockHeader));
        }

        [Test]
        public void TestPersistMessageToStore_ExtractFileStatusMessage()
        {
            var testExtractJobStore = new TestExtractJobStore();
            var message = new ExtractFileStatusMessage();
            var header = new MessageHeader();

            message.Status = ExtractFileStatus.Unknown;
            testExtractJobStore.PersistMessageToStore(message, header);

            message.Status = ExtractFileStatus.Anonymised;
            Assert.Throws<ApplicationException>(() => testExtractJobStore.PersistMessageToStore(message, header));
        }

        [Test]
        public void TestPersistMessageToStore_IsIdentifiableMessage()
        {
            var testExtractJobStore = new TestExtractJobStore();
            var message = new IsIdentifiableMessage();
            var header = new MessageHeader();

            // Must have AnonymisedFileName
            message.AnonymisedFileName = "";
            Assert.Throws<ApplicationException>(() => testExtractJobStore.PersistMessageToStore(message, header));

            // Must have report if IsIdentifiable
            message.AnonymisedFileName = "anon.dcm";
            message.IsIdentifiable = true;
            message.Report = "";
            Assert.Throws<ApplicationException>(() => testExtractJobStore.PersistMessageToStore(message, header));

            // Shouldn't have report if not IsIdentifiable
            message.IsIdentifiable = false;
            message.Report = "report";
            Assert.Throws<ApplicationException>(() => testExtractJobStore.PersistMessageToStore(message, header));

            // Otherwise ok

            message.IsIdentifiable = false;
            message.Report = "";
            testExtractJobStore.PersistMessageToStore(message, header);

            message.IsIdentifiable = true;
            message.Report = "report";
            testExtractJobStore.PersistMessageToStore(message, header);
        }

        #endregion
    }
}
