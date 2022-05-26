
using Microservices.DicomTagReader.Execution;
using Microservices.DicomTagReader.Messaging;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing;
using Smi.Common.Messages;
using Smi.Common.Tests;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Text;
using FellowOakDicom;


namespace Microservices.DicomTagReader.Tests.Messaging
{
    [TestFixture, RequiresRabbit]
    public class DicomTagReaderConsumerTests
    {
        private readonly DicomTagReaderTestHelper _helper = new();

        private IModel _mockModel;


        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _helper.SetUpSuite();

            _mockModel = Mock.Of<IModel>();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _helper.Dispose();
        }

        [SetUp]
        public void SetUp()
        {
            _helper.ResetSuite();
        }

        [TearDown]
        public void TearDown() { }


        private TagReaderBase GetMockTagReader(IFileSystem fileSystem = null)
        {
            fileSystem ??= _helper.MockFileSystem;

            return new SerialTagReader(_helper.Options.DicomTagReaderOptions, _helper.Options.FileSystemOptions, _helper.TestSeriesModel.Object, _helper.TestImageModel.Object, fileSystem);
        }

        private void CheckAckNackCounts(DicomTagReaderConsumer consumer, int desiredAckCount, int desiredNackCount)
        {
            var fatalCalled = false;
            consumer.OnFatal += (sender, args) => fatalCalled = true;

            consumer.SetModel(_mockModel);
            consumer.TestMessage(_helper.TestAccessionDirectoryMessage);

            Assert.AreEqual(desiredAckCount, consumer.AckCount);
            Assert.AreEqual(desiredNackCount, consumer.NackCount);
            Assert.False(fatalCalled);
        }

        /// <summary>
        /// Tests that a valid message is acknowledged
        /// </summary>
        [Test]
        public void TestValidMessageAck()
        {
            _helper.TestAccessionDirectoryMessage.DirectoryPath = _helper.TestDir.FullName;
            _helper.Options.FileSystemOptions.FileSystemRoot = _helper.TestDir.FullName;

            _helper.TestImageModel
                .Setup(x => x.SendMessage(It.IsAny<IMessage>(), It.IsAny<MessageHeader>(), It.IsAny<string>()))
                .Returns(new MessageHeader());

            _helper.TestSeriesModel
                .Setup(x => x.SendMessage(It.IsAny<IMessage>(), It.IsAny<MessageHeader>(), It.IsAny<string>()))
                .Returns(new MessageHeader());

            CheckAckNackCounts(new DicomTagReaderConsumer(GetMockTagReader(new FileSystem()),null), 1, 0);
        }

        /// <summary>
        /// Tests that messages are NACKd if an exception is thrown
        /// </summary>
        [Test]
        public void TestInvalidMessageNack()
        {
            _helper.MockFileSystem.AddFile(@"C:\Temp\invalidDicomFile.dcm", new MockFileData(new byte[] { 0x12, 0x34, 0x56, 0x78 }));

            _helper.TestImageModel
                .Setup(x => x.SendMessage(It.IsAny<IMessage>(), It.IsAny<MessageHeader>(), It.IsAny<string>()))
                .Returns(new MessageHeader());

            _helper.TestSeriesModel
                .Setup(x => x.SendMessage(It.IsAny<IMessage>(), It.IsAny<MessageHeader>(), It.IsAny<string>()))
                .Returns(new MessageHeader());

            CheckAckNackCounts(new DicomTagReaderConsumer(GetMockTagReader(),null), 0, 1);
        }
    }
}
