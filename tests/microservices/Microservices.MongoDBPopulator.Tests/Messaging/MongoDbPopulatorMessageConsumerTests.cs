﻿
using Microservices.MongoDBPopulator.Messaging;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing;
using Smi.Common.Messages;
using Smi.Common.Tests;
using System.Collections.Generic;
using System.Text;


namespace Microservices.MongoDBPopulator.Tests.Messaging
{
    [TestFixture, RequiresMongoDb]
    public class MongoDbPopulatorMessageConsumerTests
    {
        private MongoDbPopulatorTestHelper _helper;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _helper = new MongoDbPopulatorTestHelper();
            _helper.SetupSuite();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _helper.Dispose();
        }

        /// <summary>
        /// Test a variety of invalid messages to ensure they are NACK'd
        /// </summary>
        [Test]
        public void TestInvalidMessagesNack()
        {
            //TODO: Refactor this to a helper function in Smi.Common.Tests
            var mockDeliverArgs = Mock.Of<BasicDeliverEventArgs>();
            mockDeliverArgs.DeliveryTag = 1;
            var header = new MessageHeader();

            var consumer = new MongoDbPopulatorMessageConsumer<DicomFileMessage>(_helper.Globals.MongoDatabases.DicomStoreOptions, _helper.Globals.MongoDbPopulatorOptions, _helper.Globals.MongoDbPopulatorOptions.ImageQueueConsumerOptions);

            var nackCount = 0;
            var mockModel = new Mock<IModel>();
            mockModel.Setup(x => x.BasicNack(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<bool>())).Callback(() => ++nackCount);
            consumer.SetModel(mockModel.Object);

            mockDeliverArgs.Body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(null));
            consumer.ProcessMessage(mockDeliverArgs);
            Assert.AreEqual(1, nackCount);
        }
    }
}
