using MongoDB.Bson;
using Moq;
using NUnit.Framework;
using SmiServices.Common.Messages;
using SmiServices.Common.Options;
using SmiServices.IntegrationTests;
using SmiServices.Microservices.MongoDBPopulator;
using SmiServices.Microservices.MongoDBPopulator.Processing;
using SmiServices.UnitTests.Common;
using System;
using System.Threading;

namespace SmiServices.UnitTests.Microservices.MongoDbPopulator.Execution.Processing
{
    [TestFixture, RequiresMongoDb]
    public class MessageProcessorTests
    {
        private MongoDbPopulatorTestHelper _helper = null!;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestLogger.Setup();

            _helper = new MongoDbPopulatorTestHelper();
            _helper.SetupSuite();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _helper.Dispose();
        }

        /// <summary>
        /// Tests that the exception callback is used if an exception is thrown in ProcessMessage
        /// </summary>
        [Test]
        public void TestExceptionCallbackUsed()
        {
            var mockAdapter = Mock.Of<IMongoDbAdapter>();

            var callbackUsed = false;
            Action<Exception> exceptionCallback = (exception) => { callbackUsed = true; };

            _helper.Globals.MongoDbPopulatorOptions!.MongoDbFlushTime = 1;

            var processor = new TestMessageProcessor(_helper.Globals.MongoDbPopulatorOptions, mockAdapter, 1, exceptionCallback);

            Assert.That(processor.IsStopping, Is.False);

            Thread.Sleep(_helper.Globals.MongoDbPopulatorOptions.MongoDbFlushTime * 1000 + 100);

            Assert.Multiple(() =>
            {
                Assert.That(callbackUsed, Is.True);
                Assert.That(processor.IsStopping, Is.True);
            });
        }

        // Implementation of MessageProcessor for testing
        private class TestMessageProcessor : MessageProcessor<SeriesMessage>
        {
            public TestMessageProcessor(MongoDbPopulatorOptions options, IMongoDbAdapter mongoDbAdapter, int maxQueueSize, Action<Exception> exceptionCallback)
                : base(options, mongoDbAdapter, maxQueueSize, exceptionCallback) { }

            public override void AddToWriteQueue(SeriesMessage message, IMessageHeader header, ulong deliveryTag)
            {
                ToProcess.Enqueue(new Tuple<BsonDocument, ulong>(new BsonDocument { { "hello", "world" } }, deliveryTag));
            }

            public override void StopProcessing(string reason)
            {
                StopProcessing();
            }

            protected override void ProcessQueue()
            {
                throw new ApplicationException("Test!");
            }
        }
    }
}
