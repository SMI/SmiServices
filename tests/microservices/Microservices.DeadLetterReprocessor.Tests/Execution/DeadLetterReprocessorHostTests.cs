
using Microservices.DeadLetterReprocessor.Execution;
using Microservices.DeadLetterReprocessor.Execution.DeadLetterStorage;
using Microservices.DeadLetterReprocessor.Execution.DeadLetterStorage.MongoDocuments;
using Microservices.DeadLetterReprocessor.Options;
using MongoDB.Driver;
using NUnit.Framework;
using RabbitMQ.Client.Events;
using Smi.Common.Messages;
using Smi.Common.MongoDB;
using Smi.Common.Tests;
using Smi.Common.Tests.DeadLetterMessagingTests;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microservices.Tests.DeadLetterReprocessorTests.Execution
{
    [TestFixture, RequiresRabbit, RequiresMongoDb]
    public class DeadLetterReprocessorHostTests
    {
        private readonly DeadLetterTestHelper _testHelper = new DeadLetterTestHelper();
        private readonly DeadLetterReprocessorCliOptions _cliOptions = new DeadLetterReprocessorCliOptions();

        private IMongoDatabase _database;
        private IMongoCollection<MongoDeadLetterDocument> _deadLetterCollection;
        private IMongoCollection<MongoDeadLetterGraveyardDocument> _deadLetterGraveyard;

        #region Fixture Methods 

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _testHelper.SetUpSuite();

            MongoClient mongoClient = MongoClientHelpers
                .GetMongoClient(_testHelper.GlobalOptions.MongoDatabases.DeadLetterStoreOptions, "DeadLetterReprocessorHostTests");

            _database = mongoClient.GetDatabase(_testHelper.GlobalOptions.MongoDatabases.DeadLetterStoreOptions.DatabaseName);

            _database.DropCollection(MongoDeadLetterStore.DeadLetterStoreBaseCollectionName);
            _database.DropCollection(MongoDeadLetterStore.DeadLetterGraveyardBaseCollectionName);

            _deadLetterCollection = _database.GetCollection<MongoDeadLetterDocument>(MongoDeadLetterStore.DeadLetterStoreBaseCollectionName);
            _deadLetterGraveyard = _database.GetCollection<MongoDeadLetterGraveyardDocument>(MongoDeadLetterStore.DeadLetterGraveyardBaseCollectionName);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _testHelper.Dispose();
        }

        #endregion

        #region Test Methods

        [SetUp]
        public void SetUp()
        {
            _testHelper.ResetSuite();
        }

        [TearDown]
        public void TearDown()
        {
            _database.DropCollection(MongoDeadLetterStore.DeadLetterStoreBaseCollectionName);
            _database.DropCollection(MongoDeadLetterStore.DeadLetterGraveyardBaseCollectionName);
        }

        #endregion

        #region Tests

        /// <summary>
        /// Tests that we can first store messages from the DLQ, then reprocess them properly back onto their original queue
        /// </summary>
        [Test]
        public void TestBasicOperation()
        {
            Assert.True(_deadLetterCollection.CountDocuments(FilterDefinition<MongoDeadLetterDocument>.Empty) == 0);
            Assert.True(_deadLetterGraveyard.CountDocuments(FilterDefinition<MongoDeadLetterGraveyardDocument>.Empty) == 0);

            // Setup a test message and send it, then wait for it to be rejected
            var testMessage = new AccessionDirectoryMessage
            {
                NationalPACSAccessionNumber = "1234",
                DirectoryPath = TestContext.CurrentContext.TestDirectory
            };

            _testHelper.TestProducer.SendMessage(testMessage, null, DeadLetterTestHelper.TestRoutingKey);
            new TestTimelineAwaiter().Await(() => _testHelper.MessageRejectorConsumer.NackCount == 1);

            IMessageHeader originalHeader = _testHelper.MessageRejectorConsumer.LastHeader;
            BasicDeliverEventArgs originalArgs = _testHelper.MessageRejectorConsumer.LastArgs;

            Assert.NotNull(originalHeader);
            Assert.NotNull(originalArgs);

            // Set so the next message is accepted
            _testHelper.MessageRejectorConsumer.AcceptNext = true;

            // Check 1 message on the DLQ
            Assert.AreEqual(1, _testHelper.TestModel.MessageCount(DeadLetterTestHelper.TestDlQueueName));

            // Start the host and check message has been read from DLQ into store
            var host = new DeadLetterReprocessorHost(_testHelper.GlobalOptions, _cliOptions,loadSmiLogConfig:false);
            host.Start();

            Assert.True(_deadLetterCollection.CountDocuments(FilterDefinition<MongoDeadLetterDocument>.Empty) == 1);
            Assert.True(_deadLetterGraveyard.CountDocuments(FilterDefinition<MongoDeadLetterGraveyardDocument>.Empty) == 0);
            Assert.AreEqual(0, _testHelper.TestModel.MessageCount(DeadLetterTestHelper.TestDlQueueName));

            // Now run the host again with the FlushMessages option set
            _cliOptions.FlushMessages = true;
            host = new DeadLetterReprocessorHost(_testHelper.GlobalOptions, _cliOptions,loadSmiLogConfig:false);
            host.Start();
            System.Threading.Thread.Sleep(1000);

            // Check the message has been sent back to the exchange and received by the consumer
            Assert.True(_deadLetterCollection.CountDocuments(FilterDefinition<MongoDeadLetterDocument>.Empty) == 0);
            Assert.True(_deadLetterGraveyard.CountDocuments(FilterDefinition<MongoDeadLetterGraveyardDocument>.Empty) == 0);
            new TestTimelineAwaiter().Await(() => _testHelper.MessageRejectorConsumer.AckCount == 1);

            IMessageHeader reprocessedHeader = _testHelper.MessageRejectorConsumer.LastHeader;
            BasicDeliverEventArgs reprocessedArgs = _testHelper.MessageRejectorConsumer.LastArgs;

            Assert.NotNull(reprocessedHeader);
            Assert.NotNull(reprocessedArgs);

            // Check the received message matches the original (where expected)
            Assert.AreNotEqual(originalHeader.MessageGuid, reprocessedHeader.MessageGuid);
            Assert.AreEqual(originalHeader.ProducerExecutableName, reprocessedHeader.ProducerExecutableName);
            Assert.AreEqual(originalHeader.ProducerProcessID, reprocessedHeader.ProducerProcessID);
            Assert.AreEqual(originalHeader.OriginalPublishTimestamp, reprocessedHeader.OriginalPublishTimestamp);
            Assert.True(reprocessedHeader.IsDescendantOf(originalHeader));

            // Check the xDeathHeaders
            var reprocessedXDeathHeaders = new RabbitMqXDeathHeaders(reprocessedArgs.BasicProperties.Headers, Encoding.UTF8);

            // Can't really get the expected time, just have to copy from the result
            long expectedTime = reprocessedXDeathHeaders.XDeaths[0].Time;

            var expectedXDeathHeaders = new RabbitMqXDeathHeaders
            {
                XDeaths = new List<RabbitMqXDeath>
                {
                    new RabbitMqXDeath
                    {
                        Count = 1,
                        Exchange = DeadLetterTestHelper.RejectExchangeName,
                        Queue = DeadLetterTestHelper.RejectQueueName,
                        Reason = "rejected",
                        RoutingKeys = new List<string>
                        {
                            DeadLetterTestHelper.TestRoutingKey
                        },
                        Time = expectedTime
                    }
                },
                XFirstDeathExchange = DeadLetterTestHelper.RejectExchangeName,
                XFirstDeathQueue = DeadLetterTestHelper.RejectQueueName,
                XFirstDeathReason = "rejected",
            };

            Assert.AreEqual(expectedXDeathHeaders, reprocessedXDeathHeaders);
        }

        [Test]
        public void TestMessageRejectLoop()
        {
            Assert.True(_deadLetterCollection.CountDocuments(FilterDefinition<MongoDeadLetterDocument>.Empty) == 0);
            Assert.True(_deadLetterGraveyard.CountDocuments(FilterDefinition<MongoDeadLetterGraveyardDocument>.Empty) == 0);

            // Setup a test message and send it, then wait for it to be rejected
            var testMessage = new AccessionDirectoryMessage
            {
                NationalPACSAccessionNumber = "1234",
                DirectoryPath = TestContext.CurrentContext.TestDirectory
            };

            // Set so the host just pushes the message through each time
            _cliOptions.FlushMessages = true;

            // Send the message
            IMessageHeader originalHeader = _testHelper.TestProducer.SendMessage(testMessage, null, DeadLetterTestHelper.TestRoutingKey);

            for (var i = 0; i < _testHelper.GlobalOptions.DeadLetterReprocessorOptions.MaxRetryLimit + 1; i++)
            {
                Assert.True(_deadLetterGraveyard.CountDocuments(FilterDefinition<MongoDeadLetterGraveyardDocument>.Empty) == 0);

                int count = i + 1;
                new TestTimelineAwaiter().Await(() => _testHelper.MessageRejectorConsumer.NackCount == count);

                // Check 1 message on the DLQ
                Assert.AreEqual(1, _testHelper.TestModel.MessageCount(DeadLetterTestHelper.TestDlQueueName));

                // Start the host and check message has been read from DLQ into store
                var host = new DeadLetterReprocessorHost(_testHelper.GlobalOptions, _cliOptions,loadSmiLogConfig:false);
                host.Start();

                Assert.True(_deadLetterCollection.CountDocuments(FilterDefinition<MongoDeadLetterDocument>.Empty) == 0);
            }

            // Message should now be in the graveyard
            Assert.True(_deadLetterGraveyard.CountDocuments(FilterDefinition<MongoDeadLetterGraveyardDocument>.Empty) == 1);

            MongoDeadLetterGraveyardDocument graveyardDoc = _deadLetterGraveyard.Find(FilterDefinition<MongoDeadLetterGraveyardDocument>.Empty).Single();

            Assert.AreEqual(graveyardDoc.MessageGuid, graveyardDoc.DeadLetter.MessageGuid);
            Assert.AreEqual(originalHeader.MessageGuid, graveyardDoc.DeadLetter.Props.MessageHeader.Parents[0]);
            Assert.AreEqual("MaxRetryCount exceeded", graveyardDoc.Reason);
            Assert.True((DateTime.UtcNow - graveyardDoc.KilledAt) < TimeSpan.FromSeconds(5));
        }

        /// <summary>
        /// Tests that we can reprocess only messages from a given queue
        /// </summary>
        [Test]
        public void TestQueueFilter()
        {
            Assert.True(_deadLetterCollection.CountDocuments(FilterDefinition<MongoDeadLetterDocument>.Empty) == 0);
            Assert.True(_deadLetterGraveyard.CountDocuments(FilterDefinition<MongoDeadLetterGraveyardDocument>.Empty) == 0);

            // Setup a test message and send it, then wait for it to be rejected
            var testMessage = new AccessionDirectoryMessage
            {
                NationalPACSAccessionNumber = "1234",
                DirectoryPath = TestContext.CurrentContext.TestDirectory
            };

            _testHelper.TestProducer.SendMessage(testMessage, null, DeadLetterTestHelper.TestRoutingKey);
            new TestTimelineAwaiter().Await(() => _testHelper.MessageRejectorConsumer.NackCount == 1);

            IMessageHeader originalHeader = _testHelper.MessageRejectorConsumer.LastHeader;
            BasicDeliverEventArgs originalArgs = _testHelper.MessageRejectorConsumer.LastArgs;

            Assert.NotNull(originalHeader);
            Assert.NotNull(originalArgs);

            // Set so the next message is accepted
            _testHelper.MessageRejectorConsumer.AcceptNext = true;

            // Check 1 message on the DLQ
            Assert.AreEqual(1, _testHelper.TestModel.MessageCount(DeadLetterTestHelper.TestDlQueueName));

            _cliOptions.FlushMessages = false;

            // Start the host and check message has been read from DLQ into store
            var host = new DeadLetterReprocessorHost(_testHelper.GlobalOptions, _cliOptions,loadSmiLogConfig:false);
            host.Start();

            Assert.True(_deadLetterCollection.CountDocuments(FilterDefinition<MongoDeadLetterDocument>.Empty) == 1);
            Assert.True(_deadLetterGraveyard.CountDocuments(FilterDefinition<MongoDeadLetterGraveyardDocument>.Empty) == 0);
            Assert.AreEqual(0, _testHelper.TestModel.MessageCount(DeadLetterTestHelper.TestDlQueueName));

            host.Stop("Test over 1");

            _cliOptions.FlushMessages = true;
            _cliOptions.ReprocessFromQueue = "FakeQueueName";

            host = new DeadLetterReprocessorHost(_testHelper.GlobalOptions, _cliOptions,loadSmiLogConfig:false);
            host.Start();

            Assert.True(_deadLetterCollection.CountDocuments(FilterDefinition<MongoDeadLetterDocument>.Empty) == 1);
            Assert.True(_deadLetterGraveyard.CountDocuments(FilterDefinition<MongoDeadLetterGraveyardDocument>.Empty) == 0);
            Assert.AreEqual(0, _testHelper.TestModel.MessageCount(DeadLetterTestHelper.RejectQueueName));

            host.Stop("Test over 2");

            _cliOptions.ReprocessFromQueue = DeadLetterTestHelper.RejectQueueName;

            host = new DeadLetterReprocessorHost(_testHelper.GlobalOptions, _cliOptions,loadSmiLogConfig:false);
            host.Start();

            // Check the message has been sent back to the exchange and received by the consumer
            Assert.True(_deadLetterCollection.CountDocuments(FilterDefinition<MongoDeadLetterDocument>.Empty) == 0);
            Assert.True(_deadLetterGraveyard.CountDocuments(FilterDefinition<MongoDeadLetterGraveyardDocument>.Empty) == 0);
            new TestTimelineAwaiter().Await(() => _testHelper.MessageRejectorConsumer.AckCount == 1);

            host.Stop("Test over 3");
        }

        #endregion
    }
}
