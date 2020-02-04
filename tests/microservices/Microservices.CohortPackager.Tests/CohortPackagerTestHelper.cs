
using MongoDB.Driver;
using NUnit.Framework;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.MessageSerialization;
using Smi.Common.MongoDB;
using Smi.Common.Options;
using System;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;


namespace Microservices.CohortPackager.Tests
{
    public class CohortPackagerTestHelper
    {
        public readonly GlobalOptions Options = GlobalOptions.Load("default.yaml", TestContext.CurrentContext.TestDirectory);

        public ExtractionRequestInfoMessage TestExtractRequestInfoMessage;
        public ExtractFileCollectionInfoMessage TestFileCollectionInfoMessage;
        public ExtractFileStatusMessage TestExtractFileStatusMessage;

        public MockFileSystem MockFileSystem;

        public DateTime DefaultSubmittedDateTime;

        private MongoClient _testClient;

        private readonly Guid _testExtractIdentifier = Guid.NewGuid();


        public void SetUpSuite()
        {
            SetUpDefaults();

            _testClient = MongoClientHelpers
                .GetMongoClient(Options.MongoDatabases.ExtractionStoreOptions, "CohortPackagerTests");
        }

        public void ResetSuite()
        {
            SetUpDefaults();
            _testClient.DropDatabase(Options.MongoDatabases.ExtractionStoreOptions.DatabaseName);
        }

        private void SetUpDefaults()
        {
            DefaultSubmittedDateTime = DateTime.Now;
            Options.CohortPackagerOptions.JobWatcherTimeoutInSeconds = 9999999;

            TestExtractRequestInfoMessage = new ExtractionRequestInfoMessage
            {
                ExtractionJobIdentifier = _testExtractIdentifier,
                ProjectNumber = "1234-5678",
                ExtractionDirectory = "1234-5678/testExtraction",
                JobSubmittedAt = DefaultSubmittedDateTime,
                KeyTag = "SeriesInstanceUID",
                KeyValueCount = 1
            };

            var dispatched = new JsonCompatibleDictionary<MessageHeader, string>
            {
                SerializeableKeys = new[] { new MessageHeader() },
                SerializeableValues = new[] { "AnonymisedTestFile1.dcm" }
            };

            TestFileCollectionInfoMessage = new ExtractFileCollectionInfoMessage
            {
                ExtractionJobIdentifier = _testExtractIdentifier,
                ProjectNumber = "1234-5678",
                ExtractionDirectory = "1234-5678/testExtraction",
                JobSubmittedAt = DefaultSubmittedDateTime,

                KeyValue = "1.2.394", // Series id of IM_0001_0013.dcm
                ExtractFileMessagesDispatched = dispatched,
            };

            TestExtractFileStatusMessage = new ExtractFileStatusMessage
            {
                ExtractionJobIdentifier = Guid.NewGuid(),
                ProjectNumber = "1234-5678",
                ExtractionDirectory = "1234-5678/testExtraction",
                JobSubmittedAt = DefaultSubmittedDateTime,

                AnonymisedFileName = "AnonymisedTestFile1.dcm",
                Status = 0,
                StatusMessage = string.Empty
            };

            string extractRoot = Options.FileSystemOptions.ExtractRoot;

            MockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                {$"{extractRoot}/1234-5678/testExtraction/AnonymisedTestFile1.dcm", MockFileData.NullObject},
                {$"{extractRoot}/1234-5678/testExtraction/AnonymisedTestFile2.dcm", MockFileData.NullObject},
                {$"{extractRoot}/1234-5678/testExtraction/AnonymisedTestFile3.dcm", MockFileData.NullObject}
            });

            MockFileSystem.AddDirectory($"{extractRoot}/1234-5678/testExtraction");
        }
    }
}
