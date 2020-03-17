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
            protected override void CompleteJobImpl(Guid jobId) { }
            protected override void MarkJobFailedImpl(Guid jobId, Exception e) { }
            protected override ExtractJobInfo GetCompletedJobInfoImpl(Guid jobId) => throw new NotImplementedException();
            protected override IEnumerable<Tuple<string, int>> GetCompletedJobRejectionsImpl(Guid jobId) => throw new NotImplementedException();
            protected override IEnumerable<Tuple<string, string>> GetCompletedJobAnonymisationFailuresImpl(Guid jobId) => throw new NotImplementedException();
            protected override IEnumerable<Tuple<string, string>> GetCompletedJobVerificationFailuresImpl(Guid jobId) => throw new NotImplementedException();
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
            Assert.Throws<ApplicationException>(() => testExtractJobStore.PersistMessageToStore(message, header));

            message.Status = ExtractFileStatus.Anonymised;
            Assert.Throws<ApplicationException>(() => testExtractJobStore.PersistMessageToStore(message, header));

            message.Status = ExtractFileStatus.ErrorWontRetry;
            testExtractJobStore.PersistMessageToStore(message, header);

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

            // Must have report
            message.AnonymisedFileName = "anon.dcm";
            message.Report = "";
            Assert.Throws<ApplicationException>(() => testExtractJobStore.PersistMessageToStore(message, header));

            // Otherwise ok
            message.Report = "report";
            testExtractJobStore.PersistMessageToStore(message, header);
        }

        [Test]
        public void TestMarkJobCompleted()
        {
            var store = new TestExtractJobStore();

            Assert.Throws<ArgumentNullException>(() => store.MarkJobCompleted(Guid.Empty));

            store.MarkJobCompleted(Guid.NewGuid());
        }

        [Test]
        public void TestMarkJobFailed()
        {
            var store = new TestExtractJobStore();

            Assert.Throws<ArgumentNullException>(() => store.MarkJobFailed(Guid.Empty, new Exception()));
            Assert.Throws<ArgumentNullException>(() => store.MarkJobFailed(Guid.NewGuid(), null));

            store.MarkJobFailed(Guid.NewGuid(), new Exception());
        }

        #endregion
    }
}
