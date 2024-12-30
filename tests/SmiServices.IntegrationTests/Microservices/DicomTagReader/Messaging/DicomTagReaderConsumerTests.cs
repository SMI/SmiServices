
using Moq;
using NUnit.Framework;
using RabbitMQ.Client;
using SmiServices.Common.Messages;
using SmiServices.Common.Options;
using SmiServices.Microservices.DicomTagReader.Execution;
using SmiServices.Microservices.DicomTagReader.Messaging;
using SmiServices.UnitTests.Microservices.DicomTagReader;
using System;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace SmiServices.IntegrationTests.Microservices.DicomTagReader.Messaging
{
    [TestFixture, RequiresRabbit]
    public class DicomTagReaderConsumerTests
    {
        private readonly DicomTagReaderTestHelper _helper = new();

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _helper.SetUpSuite();
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


        private TagReaderBase GetMockTagReader(IFileSystem? fileSystem = null)
        {
            fileSystem ??= _helper.MockFileSystem;

            return new SerialTagReader(_helper.Options.DicomTagReaderOptions!, _helper.Options.FileSystemOptions!, _helper.TestSeriesModel.Object, _helper.TestImageModel.Object, fileSystem);
        }

        private void CheckAckNackCounts(DicomTagReaderConsumer consumer, int desiredAckCount, int desiredNackCount)
        {
            var fatalCalled = false;
            consumer.OnFatal += (sender, args) => fatalCalled = true;

            consumer.TestMessage(_helper.TestAccessionDirectoryMessage);

            Assert.Multiple(() =>
            {
                Assert.That(consumer.AckCount, Is.EqualTo(desiredAckCount));
                Assert.That(consumer.NackCount, Is.EqualTo(desiredNackCount));
                Assert.That(fatalCalled, Is.False);
            });
        }

        /// <summary>
        /// Tests that a valid message is acknowledged
        /// </summary>
        [Test]
        public void TestValidMessageAck()
        {
            _helper.TestAccessionDirectoryMessage.DirectoryPath = _helper.TestDir.FullName;
            _helper.Options.FileSystemOptions!.FileSystemRoot = _helper.TestDir.FullName;

            _helper.TestImageModel
                .Setup(x => x.SendMessage(It.IsAny<IMessage>(), It.IsAny<MessageHeader>(), It.IsAny<string>()))
                .Returns(new MessageHeader());

            _helper.TestSeriesModel
                .Setup(x => x.SendMessage(It.IsAny<IMessage>(), It.IsAny<MessageHeader>(), It.IsAny<string>()))
                .Returns(new MessageHeader());

            CheckAckNackCounts(new DicomTagReaderConsumer(GetMockTagReader(new FileSystem()), Mock.Of<GlobalOptions>()), 1, 0);
        }

        /// <summary>
        /// Tests that messages are NACKd if an exception is thrown
        /// </summary>
        [Test]
        public void TestInvalidMessageNack()
        {
            _helper.MockFileSystem.AddFile(@"C:\Temp\invalidDicomFile.dcm", new MockFileData([0x12, 0x34, 0x56, 0x78]));

            _helper.TestImageModel
                .Setup(x => x.SendMessage(It.IsAny<IMessage>(), It.IsAny<MessageHeader>(), It.IsAny<string>()))
                .Returns(new MessageHeader());

            _helper.TestSeriesModel
                .Setup(x => x.SendMessage(It.IsAny<IMessage>(), It.IsAny<MessageHeader>(), It.IsAny<string>()))
                .Returns(new MessageHeader());

            CheckAckNackCounts(new DicomTagReaderConsumer(GetMockTagReader(), Mock.Of<GlobalOptions>()), 0, 1);
        }
    }
}
