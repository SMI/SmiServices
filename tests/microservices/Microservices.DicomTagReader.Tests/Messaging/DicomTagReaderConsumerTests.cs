
using Microservices.DicomTagReader.Execution;
using Microservices.DicomTagReader.Messaging;
using Moq;
using NUnit.Framework;
using RabbitMQ.Client;
using Smi.Common.Messages;
using Smi.Common.Options;
using Smi.Common.Tests;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace Microservices.DicomTagReader.Tests.Messaging
{
    [TestFixture, RequiresRabbit]
    public class DicomTagReaderConsumerTests
    {
        private readonly DicomTagReaderTestHelper _helper = new();

        private IModel? _mockModel = null;


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
            _mockModel?.Dispose();
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
            if (_mockModel is null)
            {
                Assert.Fail("Mock model not set");
                return;
            }

            var fatalCalled = false;
            consumer.OnFatal += (sender, args) => fatalCalled = true;

            consumer.SetModel(_mockModel);
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
            _helper.MockFileSystem.AddFile(@"C:\Temp\invalidDicomFile.dcm", new MockFileData(new byte[] { 0x12, 0x34, 0x56, 0x78 }));

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
