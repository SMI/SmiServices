using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Linq.Expressions;
using Moq;
using NUnit.Framework;
using Smi.Common.Helpers;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.Messaging;
using Smi.Common.Options;
using Smi.Common.Tests;


namespace Applications.ExtractImages.Tests
{
    public class ExtractionMessageSenderTests
    {
        #region Fixture Methods

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestLogger.Setup();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() { }

        #endregion

        #region Test Methods

        [SetUp]
        public void SetUp() { }

        [TearDown]
        public void TearDown() { }

        private class TestConsoleInput : IConsoleInput
        {
            private string? _line;

            public TestConsoleInput(string line)
            {
                _line = line;
            }

            public string? GetNextLine()
            {
                string? line = _line;
                _line = null;
                return line;
            }
        }

        #endregion

        #region Tests

        [TestCase(true)]
        [TestCase(false)]
        public void HappyPath_Interactive(bool confirm)
        {
            Expression<Func<IProducerModel, IMessageHeader?>> expr = x => x.SendMessage(It.IsAny<IMessage>(), null, It.IsAny<string>());

            var mockExtractionRequestProducer = new Mock<IProducerModel>(MockBehavior.Strict);
            mockExtractionRequestProducer.Setup(expr).Returns((IMessageHeader?)null);

            var mockExtractionRequestInfoProducer = new Mock<IProducerModel>(MockBehavior.Strict);
            mockExtractionRequestInfoProducer.Setup(expr).Returns((IMessageHeader?)null);

            var fs = new MockFileSystem();
            const string extractRoot = "extractRoot";
            var extractDir = fs.Path.Join("proj1", "extractions", "foo");

            var processor = new ExtractionMessageSender(
                new ExtractImagesOptions(),
                new ExtractImagesCliOptions { ProjectId = "1234-5678" },
                mockExtractionRequestProducer.Object,
                mockExtractionRequestInfoProducer.Object,
                fs,
                extractRoot,
                extractDir,
                new TestDateTimeProvider(),
                new TestConsoleInput(confirm ? "y" : "n")
            );

            var idList = new List<string> { "foo" };
            processor.SendMessages(ExtractionKey.StudyInstanceUID, idList);

            if (confirm)
            {
                mockExtractionRequestProducer.Verify(expr, Times.Once);
                mockExtractionRequestInfoProducer.Verify(expr, Times.Once);

                Assert.That(fs.File.Exists(fs.Path.Join(extractRoot, extractDir, "jobId.txt")), Is.True);
            }
            else
            {
                mockExtractionRequestProducer.Verify(expr, Times.Never);
                mockExtractionRequestInfoProducer.Verify(expr, Times.Never);

                Assert.That(fs.Directory.Exists(extractDir), Is.False);
            }
        }

        [Test]
        public void HappyPath_NonInteractive()
        {
            Expression<Func<IProducerModel, IMessageHeader?>> expr = x => x.SendMessage(It.IsAny<IMessage>(), null, It.IsAny<string>());

            var mockExtractionRequestProducer = new Mock<IProducerModel>(MockBehavior.Strict);
            mockExtractionRequestProducer.Setup(expr).Returns((IMessageHeader?)null);

            var mockExtractionRequestInfoProducer = new Mock<IProducerModel>(MockBehavior.Strict);
            mockExtractionRequestInfoProducer.Setup(expr).Returns((IMessageHeader?)null);

            var fs = new MockFileSystem();
            const string extractRoot = "extractRoot";
            var extractDir = fs.Path.Join("proj1", "extractions", "foo");

            var processor = new ExtractionMessageSender(
                new ExtractImagesOptions(),
                new ExtractImagesCliOptions { ProjectId = "1234-5678", NonInteractive = true },
                mockExtractionRequestProducer.Object,
                mockExtractionRequestInfoProducer.Object,
                fs,
                extractRoot,
                extractDir,
                new TestDateTimeProvider(),
                new RealConsoleInput()
            );

            var idList = new List<string> { "foo" };
            processor.SendMessages(ExtractionKey.StudyInstanceUID, idList);

            mockExtractionRequestProducer.Verify(expr, Times.Once);
            mockExtractionRequestInfoProducer.Verify(expr, Times.Once);

            Assert.That(fs.File.Exists(fs.Path.Join(extractRoot, extractDir, "jobId.txt")), Is.True);
        }

        [TestCase("")]
        [TestCase("  ")]
        public void ExtractionDirs_AreValidated(string dir)
        {
            var exc = Assert.Throws<ArgumentException>(() =>
            {
                var _ = new ExtractionMessageSender(
                    new ExtractImagesOptions(),
                    new ExtractImagesCliOptions(),
                    new Mock<IProducerModel>(MockBehavior.Loose).Object,
                    new Mock<IProducerModel>(MockBehavior.Loose).Object,
                    new FileSystem(),
                    "extractionRoot",
                    dir,
                    new TestDateTimeProvider(),
                    new RealConsoleInput()
                );
            });
            Assert.That(exc!.Message, Is.EqualTo("extractionDir"));

            exc = Assert.Throws<ArgumentException>(() =>
            {
                var _ = new ExtractionMessageSender(
                    new ExtractImagesOptions(),
                    new ExtractImagesCliOptions(),
                    new Mock<IProducerModel>(MockBehavior.Loose).Object,
                    new Mock<IProducerModel>(MockBehavior.Loose).Object,
                    new FileSystem(),
                    dir,
                    "extractionDir",
                    new TestDateTimeProvider(),
                    new RealConsoleInput()
                );
            });
            Assert.That(exc!.Message, Is.EqualTo("extractionRoot"));
        }

        [TestCase("")]
        [TestCase("  ")]
        public void ProjectId_IsValidated(string projectId)
        {
            var exc = Assert.Throws<ArgumentException>(() =>
            {
                var _ = new ExtractionMessageSender(
                    new ExtractImagesOptions(),
                    new ExtractImagesCliOptions { ProjectId = projectId },
                    new Mock<IProducerModel>(MockBehavior.Loose).Object,
                    new Mock<IProducerModel>(MockBehavior.Loose).Object,
                    new FileSystem(),
                    "extractRoot",
                    "extractDir",
                    new TestDateTimeProvider(),
                    new RealConsoleInput()
                );
            });
            Assert.That(exc!.Message, Is.EqualTo("ProjectId"));
        }

        [Test]
        public void MaxIdentifiersPerMessage_IsValidated()
        {
            var exc = Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var _ = new ExtractionMessageSender(
                    new ExtractImagesOptions { MaxIdentifiersPerMessage = 0 },
                    new ExtractImagesCliOptions(),
                    new Mock<IProducerModel>(MockBehavior.Loose).Object,
                    new Mock<IProducerModel>(MockBehavior.Loose).Object,
                    new FileSystem(),
                    "extractRoot",
                    "extractDir",
                    new TestDateTimeProvider(),
                    new RealConsoleInput()
                );
            });
            Assert.That(exc!.Message, Does.EndWith("(Parameter 'MaxIdentifiersPerMessage')"));
        }


        [Test]
        public void IdList_IsNotEmpty()
        {
            var sender = new ExtractionMessageSender(
                new ExtractImagesOptions(),
                new ExtractImagesCliOptions { ProjectId = "1234-5678" },
                new Mock<IProducerModel>(MockBehavior.Loose).Object,
                new Mock<IProducerModel>(MockBehavior.Loose).Object,
                new FileSystem(),
                    "extractRoot",
                "extractDir",
                new TestDateTimeProvider(),
                new RealConsoleInput()
            );

            var exc = Assert.Throws<ArgumentException>(() =>
            {
                sender.SendMessages(ExtractionKey.StudyInstanceUID, new List<string>());
            });
            Assert.That(exc!.Message, Is.EqualTo("ID list is empty"));
        }

        [Test]
        public void ListChunking_CorrectIds()
        {
            Expression<Func<IProducerModel, IMessageHeader?>> expr = x => x.SendMessage(It.IsAny<IMessage>(), null, It.IsAny<string>());

            var calledWith = new List<string>();

            var mockExtractionRequestProducer = new Mock<IProducerModel>(MockBehavior.Strict);
            mockExtractionRequestProducer
                .Setup(expr)
                .Returns((IMessageHeader?)null)
                .Callback((IMessage msg, IMessageHeader _, string __) =>
                {
                    calledWith.AddRange(((ExtractionRequestMessage)msg).ExtractionIdentifiers);
                });

            var mockExtractionRequestInfoProducer = new Mock<IProducerModel>(MockBehavior.Strict);
            mockExtractionRequestInfoProducer.Setup(expr).Returns((IMessageHeader?)null);

            var processor = new ExtractionMessageSender(
                new ExtractImagesOptions { MaxIdentifiersPerMessage = 1 },
                new ExtractImagesCliOptions { ProjectId = "1234-5678", NonInteractive = true },
                mockExtractionRequestProducer.Object,
                mockExtractionRequestInfoProducer.Object,
                new FileSystem(),
                    "extractRoot",
                "extractDir",
                new TestDateTimeProvider(),
                new RealConsoleInput()
            );

            List<string> idList = Enumerable.Range(0, 5).Select(x => x.ToString()).ToList();
            processor.SendMessages(ExtractionKey.StudyInstanceUID, idList);

            mockExtractionRequestProducer.Verify(expr, Times.Exactly(5));
            mockExtractionRequestInfoProducer.Verify(expr, Times.Once);

            Assert.That(idList.SequenceEqual(calledWith), Is.True);
        }

        [TestCase(1, 1, 1)] // nIds = maxPerMessage  => 1 message
        [TestCase(1, 10, 1)] // nIds < maxPerMessage => 1 message
        [TestCase(2, 1, 2)] // nIds > maxPerMessage => 2 messages
        public void ListChunking_EdgeCases(int nIds, int maxPerMessage, int expectedMessages)
        {
            Expression<Func<IProducerModel, IMessageHeader?>> expr = x => x.SendMessage(It.IsAny<IMessage>(), null, It.IsAny<string>());

            var mockExtractionRequestProducer = new Mock<IProducerModel>(MockBehavior.Strict);
            mockExtractionRequestProducer.Setup(expr).Returns((IMessageHeader?)null);

            var mockExtractionRequestInfoProducer = new Mock<IProducerModel>(MockBehavior.Strict);
            mockExtractionRequestInfoProducer.Setup(expr).Returns((IMessageHeader?)null);

            var processor = new ExtractionMessageSender(
                new ExtractImagesOptions { MaxIdentifiersPerMessage = maxPerMessage },
                new ExtractImagesCliOptions { ProjectId = "1234-5678", NonInteractive = true },
                mockExtractionRequestProducer.Object,
                mockExtractionRequestInfoProducer.Object,
                new FileSystem(),
                    "extractRoot",
                "extractDir",
                new TestDateTimeProvider(),
                new RealConsoleInput()
            );

            List<string> idList = Enumerable.Range(0, nIds).Select(x => x.ToString()).ToList();
            processor.SendMessages(ExtractionKey.StudyInstanceUID, idList);

            mockExtractionRequestProducer.Verify(expr, Times.Exactly(expectedMessages));
            mockExtractionRequestInfoProducer.Verify(expr, Times.Once);
        }

        #endregion
    }
}
