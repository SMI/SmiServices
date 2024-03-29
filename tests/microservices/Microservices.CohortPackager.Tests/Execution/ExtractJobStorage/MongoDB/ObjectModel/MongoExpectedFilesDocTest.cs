﻿using System;
using System.Collections.Generic;
using System.Reflection;
using Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB.ObjectModel;
using Moq;
using NUnit.Framework;
using Smi.Common.Helpers;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.MessageSerialization;
using Smi.Common.Tests;


namespace Microservices.CohortPackager.Tests.Execution.ExtractJobStorage.MongoDB.ObjectModel
{
    [TestFixture]
    public class MongoExpectedFilesDocTest
    {
        private readonly DateTimeProvider _dateTimeProvider = new TestDateTimeProvider();

        private readonly MessageHeader _testHeader = new()
        {
            Parents = new[] { Guid.NewGuid(), },
        };

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

        #endregion

        #region Tests

        [Test]
        public void TestMongoExpectedFilesDoc_SettersAvailable()
        {
            foreach (PropertyInfo p in typeof(MongoExpectedFilesDoc).GetProperties())
                Assert.True(p.CanWrite, $"Property '{p.Name}' is not writeable");
        }

        [Test]
        public void TestMongoExpectedFilesDoc_FromMessage()
        {
            var mockMessage = new Mock<ExtractFileCollectionInfoMessage>();
            mockMessage.Object.KeyValue = "TestKey";
            Guid jobId = Guid.NewGuid();
            mockMessage.Object.ExtractionJobIdentifier = jobId;
            var header1 = new MessageHeader();
            var header2 = new MessageHeader();
            mockMessage.Object.ExtractFileMessagesDispatched = new JsonCompatibleDictionary<MessageHeader, string>
            {
                { header1, "AnonFile1.dcm"},
                { header2, "AnonFile2.dcm"},
            };
            mockMessage.Object.RejectionReasons = new Dictionary<string, int>
            {
                { "Reject1", 1 },
                { "Reject2", 2 },
            };

            MongoExpectedFilesDoc doc = MongoExpectedFilesDoc.FromMessage(mockMessage.Object, _testHeader, _dateTimeProvider);

            var expected = new MongoExpectedFilesDoc(
                MongoExtractionMessageHeaderDoc.FromMessageHeader(jobId, _testHeader, _dateTimeProvider),
                "TestKey",
                new HashSet<MongoExpectedFileInfoDoc>
                {
                    new MongoExpectedFileInfoDoc(header1.MessageGuid,"AnonFile1.dcm"),
                    new MongoExpectedFileInfoDoc(header2.MessageGuid,"AnonFile2.dcm"),
                },
                MongoRejectedKeyInfoDoc.FromMessage(mockMessage.Object, _testHeader, _dateTimeProvider));

            Assert.AreEqual(expected, doc);
        }

        [Test]
        public void TestMongoExpectedFilesDoc_Equality()
        {
            var expectedFiles = new HashSet<MongoExpectedFileInfoDoc>
            {
                new MongoExpectedFileInfoDoc(Guid.NewGuid(), "anon1.dcm"),
                new MongoExpectedFileInfoDoc(Guid.NewGuid(), "anon2.dcm"),
            };
            Guid jobId = Guid.NewGuid();
            var rejectedKeys = new MongoRejectedKeyInfoDoc(
                MongoExtractionMessageHeaderDoc.FromMessageHeader(jobId, _testHeader, _dateTimeProvider),
                new Dictionary<string, int>
                {
                    {"reject-1", 1 },
                    {"reject-2", 2 },
                });

            var doc1 = new MongoExpectedFilesDoc(
                MongoExtractionMessageHeaderDoc.FromMessageHeader(jobId, _testHeader, _dateTimeProvider),
                "TestKey",
                expectedFiles,
                rejectedKeys);
            var doc2 = new MongoExpectedFilesDoc(
                MongoExtractionMessageHeaderDoc.FromMessageHeader(jobId, _testHeader, _dateTimeProvider),
                "TestKey",
                expectedFiles,
                rejectedKeys);

            Assert.AreEqual(doc1, doc2);
        }

        [Test]
        public void TestMongoExpectedFilesDoc_GetHashCode()
        {
            var expectedFiles = new HashSet<MongoExpectedFileInfoDoc>
            {
                new MongoExpectedFileInfoDoc(Guid.NewGuid(), "anon1.dcm"),
                new MongoExpectedFileInfoDoc(Guid.NewGuid(), "anon2.dcm"),
            };
            Guid jobId = Guid.NewGuid();
            var rejectedKeys = new MongoRejectedKeyInfoDoc(
                MongoExtractionMessageHeaderDoc.FromMessageHeader(jobId, _testHeader, _dateTimeProvider),
                new Dictionary<string, int>
                {
                    {"reject-1", 1 },
                    {"reject-2", 2 },
                }
            );

            var doc1 = new MongoExpectedFilesDoc(
                MongoExtractionMessageHeaderDoc.FromMessageHeader(jobId, _testHeader, _dateTimeProvider),
                "TestKey",
                expectedFiles,
                rejectedKeys);
            var doc2 = new MongoExpectedFilesDoc(
                MongoExtractionMessageHeaderDoc.FromMessageHeader(jobId, _testHeader, _dateTimeProvider),
                "TestKey",
                expectedFiles,
                rejectedKeys);

            Assert.AreEqual(doc1.GetHashCode(), doc2.GetHashCode());
        }

        [Test]
        public void TestMongoExpectedFileInfoDoc_SettersAvailable()
        {
            foreach (PropertyInfo p in typeof(MongoExpectedFileInfoDoc).GetProperties())
                Assert.True(p.CanWrite, $"Property '{p.Name}' is not writeable");
        }

        [Test]
        public void TestMongoExpectedFileInfoDoc_Equality()
        {
            Guid guid = Guid.NewGuid();
            var doc1 = new MongoExpectedFileInfoDoc(guid, "AnonFile1.dcm");
            var doc2 = new MongoExpectedFileInfoDoc(guid, "AnonFile1.dcm");
            Assert.AreEqual(doc1, doc2);
        }

        [Test]
        public void TestMongoExpectedFileInfoDoc_GetHashcode()
        {
            Guid guid = Guid.NewGuid();
            var doc1 = new MongoExpectedFileInfoDoc(guid, "AnonFile1.dcm");
            var doc2 = new MongoExpectedFileInfoDoc(guid, "AnonFile1.dcm");
            Assert.AreEqual(doc1.GetHashCode(), doc2.GetHashCode());
        }
        
        [Test]
        public void TestMongoRejectedKeyInfoDoc_SettersAvailable()
        {
            foreach (PropertyInfo p in typeof(MongoRejectedKeyInfoDoc).GetProperties())
                Assert.True(p.CanWrite, $"Property '{p.Name}' is not writeable");
        }

        [Test]
        public void TestMongoRejectedKeyInfoDoc_FromMessage()
        {
            var mockMessage = new Mock<ExtractFileCollectionInfoMessage>();
            Guid jobId = Guid.NewGuid();
            mockMessage.Object.ExtractionJobIdentifier = jobId;
            mockMessage.Object.RejectionReasons = new Dictionary<string, int>
            {
                {"Reject1", 1 },
                {"Reject2", 2 },
            };

            MongoRejectedKeyInfoDoc doc = MongoRejectedKeyInfoDoc.FromMessage(mockMessage.Object, _testHeader, _dateTimeProvider);

            var expected = new MongoRejectedKeyInfoDoc(
                MongoExtractionMessageHeaderDoc.FromMessageHeader(jobId, _testHeader, _dateTimeProvider),
                new Dictionary<string, int>
                {
                    {"Reject1", 1},
                    {"Reject2", 2},
                });

            Assert.AreEqual(expected, doc);
        }

        [Test]
        public void TestMongoRejectedKeyInfoDoc_Equality()
        {
            Guid guid = Guid.NewGuid();
            var rejectReasons = new Dictionary<string, int>
            {
                {"Reject1", 1 },
                {"Reject2", 2 },
            };

            var doc1 = new MongoRejectedKeyInfoDoc(MongoExtractionMessageHeaderDoc.FromMessageHeader(guid, _testHeader, _dateTimeProvider), rejectReasons);
            var doc2 = new MongoRejectedKeyInfoDoc(MongoExtractionMessageHeaderDoc.FromMessageHeader(guid, _testHeader, _dateTimeProvider), rejectReasons);

            Assert.AreEqual(doc1, doc2);
        }

        [Test]
        public void TestMongoRejectedKeyInfoDoc_GetHashCode()
        {
            Guid guid = Guid.NewGuid();
            var rejectReasons = new Dictionary<string, int>
            {
                {"Reject1", 1 },
                {"Reject2", 2 },
            };

            var doc1 = new MongoRejectedKeyInfoDoc(MongoExtractionMessageHeaderDoc.FromMessageHeader(guid, _testHeader, _dateTimeProvider), rejectReasons);
            var doc2 = new MongoRejectedKeyInfoDoc(MongoExtractionMessageHeaderDoc.FromMessageHeader(guid, _testHeader, _dateTimeProvider), rejectReasons);

            Assert.AreEqual(doc1.GetHashCode(), doc2.GetHashCode());
        }

        #endregion
    }
}
