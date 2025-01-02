using NUnit.Framework;
using SmiServices.Common.Messages;
using SmiServices.Common.Messages.Extraction;
using SmiServices.Microservices.CohortPackager.ExtractJobStorage;
using SmiServices.UnitTests.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;


namespace SmiServices.UnitTests.Microservices.CohortPackager.Execution.ExtractJobStorage;


[TestFixture]
public class ExtractJobStoreTest
{
    #region Fixture Methods 

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

    private class TestExtractJobStore : ExtractJobStore
    {
        public override ConcurrentQueue<Tuple<IMessageHeader, ulong>> ProcessedVerificationMessages => throw new NotImplementedException();

        protected override void PersistMessageToStoreImpl(ExtractionRequestInfoMessage message, IMessageHeader header) { }
        protected override void PersistMessageToStoreImpl(ExtractFileCollectionInfoMessage collectionInfoMessage, IMessageHeader header) => throw new NotImplementedException();
        protected override void PersistMessageToStoreImpl(ExtractedFileStatusMessage message, IMessageHeader header) { }
        protected override void PersistMessageToStoreImpl(ExtractedFileVerificationMessage message, IMessageHeader header) { }
        protected override List<ExtractJobInfo> GetReadyJobsImpl(Guid specificJobId = new Guid()) => throw new NotImplementedException();
        protected override void CompleteJobImpl(Guid jobId) { }
        protected override void MarkJobFailedImpl(Guid jobId, Exception e) { }
        protected override CompletedExtractJobInfo GetCompletedJobInfoImpl(Guid jobId) => throw new NotImplementedException();
        protected override IEnumerable<ExtractionIdentifierRejectionInfo> GetCompletedJobRejectionsImpl(Guid jobId) => throw new NotImplementedException();
        protected override IEnumerable<FileAnonFailureInfo> GetCompletedJobAnonymisationFailuresImpl(Guid jobId) => throw new NotImplementedException();
        protected override IEnumerable<FileVerificationFailureInfo> GetCompletedJobVerificationFailuresImpl(Guid jobId) => throw new NotImplementedException();
        protected override IEnumerable<string> GetCompletedJobMissingFileListImpl(Guid jobId) => ["missing"];
        protected override void AddToWriteQueueImpl(ExtractedFileVerificationMessage message, IMessageHeader header, ulong tag) => throw new NotImplementedException();
        public override void ProcessVerificationMessageQueue() => throw new NotImplementedException();
    }

    #endregion

    #region Test Methods

    [Test]
    public void TestPersistMessageToStore_ExtractFileStatusMessage()
    {
        var testExtractJobStore = new TestExtractJobStore();
        var message = new ExtractedFileStatusMessage();
        var header = new MessageHeader();

        message.Status = ExtractedFileStatus.None;
        Assert.Throws<ApplicationException>(() => testExtractJobStore.PersistMessageToStore(message, header));

        message.Status = ExtractedFileStatus.Anonymised;
        Assert.Throws<ApplicationException>(() => testExtractJobStore.PersistMessageToStore(message, header));

        message.Status = ExtractedFileStatus.ErrorWontRetry;
        testExtractJobStore.PersistMessageToStore(message, header);

    }

    [Test]
    public void TestPersistMessageToStore_IsIdentifiableMessage()
    {
        var testExtractJobStore = new TestExtractJobStore();
        var header = new MessageHeader();

        // Must have AnonymisedFileName
        var message = new ExtractedFileVerificationMessage
        {
            OutputFilePath = ""
        };
        Assert.Throws<ApplicationException>(() => testExtractJobStore.PersistMessageToStore(message, header));

        // Report shouldn't be an empty string or null
        message = new ExtractedFileVerificationMessage
        {
            OutputFilePath = "anon.dcm",
            Report = ""
        };
        Assert.Throws<ApplicationException>(() => testExtractJobStore.PersistMessageToStore(message, header));

        // Report needs to contain content if marked as IsIdentifiable
        message = new ExtractedFileVerificationMessage
        {
            OutputFilePath = "anon.dcm",
            Status = VerifiedFileStatus.IsIdentifiable,
            Report = "[]"
        };
        Assert.Throws<ApplicationException>(() => testExtractJobStore.PersistMessageToStore(message, header));
        // NOTE(rkm 2020-07-23) The actual report content is verified to be valid the message consumer, so don't need to re-check here
        message.Report = "['foo': 'bar']";
        testExtractJobStore.PersistMessageToStore(message, header);

        // Report can be empty if not marked as IsIdentifiable
        message = new ExtractedFileVerificationMessage
        {
            OutputFilePath = "anon.dcm",
            Status = VerifiedFileStatus.NotIdentifiable,
            Report = "[]"
        };
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

        store.MarkJobFailed(Guid.NewGuid(), new Exception());
    }

    [Test]
    public void Test_GetCompletedJobMissingFileList()
    {
        var store = new TestExtractJobStore();
        Assert.Throws<ArgumentNullException>(() => store.GetCompletedJobMissingFileList(default));
        Assert.That(store.GetCompletedJobMissingFileList(Guid.NewGuid()), Is.EqualTo(new[] { "missing" }));
    }

    #endregion
}
