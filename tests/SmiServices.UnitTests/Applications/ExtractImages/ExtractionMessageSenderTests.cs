using Moq;
using NUnit.Framework;
using SmiServices.Applications.ExtractImages;
using SmiServices.Common.Helpers;
using SmiServices.Common.Messages;
using SmiServices.Common.Messages.Extraction;
using SmiServices.Common.Messaging;
using SmiServices.Common.Options;
using SmiServices.UnitTests.Common;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Linq.Expressions;
using SmiServices.UnitTests.Common.Messaging;
using NSubstitute.ExceptionExtensions;


namespace SmiServices.UnitTests.Applications.ExtractImages
{
    public class ExtractionMessageSenderTests
    {
        #region Fixture Methods

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
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
            var mockExtractionRequestProducer = new TestProducer<ExtractionRequestMessage>();
            var mockExtractionRequestInfoProducer = new TestProducer<ExtractionRequestInfoMessage>();
            var fs = new MockFileSystem();
            const string extractRoot = "extractRoot";
            var extractDir = fs.Path.Join("proj1", "extractions", "foo");

            var processor = new ExtractionMessageSender(
                new ExtractImagesOptions(),
                new ExtractImagesCliOptions { ProjectId = "1234-5678" },
                mockExtractionRequestProducer,
                mockExtractionRequestInfoProducer,
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
                Assert.Multiple(() =>
                {
                    Assert.That(mockExtractionRequestProducer.TotalSent, Is.EqualTo(1));
                    Assert.That(mockExtractionRequestInfoProducer.TotalSent, Is.EqualTo(1));
                    Assert.That(fs.File.Exists(fs.Path.Join(extractRoot, extractDir, "jobId.txt")), Is.True);
                });
            }
            else
            {
                Assert.Multiple(() =>
                {
                    Assert.That(mockExtractionRequestProducer.TotalSent, Is.EqualTo(0));
                    Assert.That(mockExtractionRequestInfoProducer.TotalSent, Is.EqualTo(0));
                    Assert.That(fs.Directory.Exists(extractDir), Is.False);
                });
            }
        }

        [Test]
        public void HappyPath_NonInteractive()
        {
            var mockExtractionRequestProducer = new TestProducer<ExtractionRequestMessage>();
            var mockExtractionRequestInfoProducer = new TestProducer<ExtractionRequestInfoMessage>();
            var fs = new MockFileSystem();
            const string extractRoot = "extractRoot";
            var extractDir = fs.Path.Join("proj1", "extractions", "foo");

            var processor = new ExtractionMessageSender(
                new ExtractImagesOptions(),
                new ExtractImagesCliOptions { ProjectId = "1234-5678", NonInteractive = true },
                mockExtractionRequestProducer,
                mockExtractionRequestInfoProducer,
                fs,
                extractRoot,
                extractDir,
                new TestDateTimeProvider(),
                new RealConsoleInput()
            );

            var idList = new List<string> { "foo" };
            processor.SendMessages(ExtractionKey.StudyInstanceUID, idList);

            Assert.Multiple(() =>
            {
                Assert.That(mockExtractionRequestProducer.TotalSent, Is.EqualTo(1));
                Assert.That(mockExtractionRequestInfoProducer.TotalSent, Is.EqualTo(1));
                Assert.That(fs.File.Exists(fs.Path.Join(extractRoot, extractDir, "jobId.txt")), Is.True);
            });
        }

        [TestCase("")]
        [TestCase("  ")]
        public void ExtractionDirs_AreValidated(string dir)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var _ = new ExtractionMessageSender(
                    new ExtractImagesOptions(),
                    new ExtractImagesCliOptions(),
                    new TestProducer<ExtractionRequestMessage>(),
                    new TestProducer<ExtractionRequestInfoMessage>(),
                    new FileSystem(),
                    "extractionRoot",
                    dir,
                    new TestDateTimeProvider(),
                    new RealConsoleInput()
                );
            });

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var _ = new ExtractionMessageSender(
                    new ExtractImagesOptions(),
                    new ExtractImagesCliOptions(),
                    new TestProducer<ExtractionRequestMessage>(),
                    new TestProducer<ExtractionRequestInfoMessage>(),
                    new FileSystem(),
                    dir,
                    "extractionDir",
                    new TestDateTimeProvider(),
                    new RealConsoleInput()
                );
            });
        }

        [TestCase("")]
        [TestCase("  ")]
        public void ProjectId_IsValidated(string projectId)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var _ = new ExtractionMessageSender(
                    new ExtractImagesOptions(),
                    new ExtractImagesCliOptions { ProjectId = projectId },
                    new TestProducer<ExtractionRequestMessage>(),
                    new TestProducer<ExtractionRequestInfoMessage>(),
                    new FileSystem(),
                    "extractRoot",
                    "extractDir",
                    new TestDateTimeProvider(),
                    new RealConsoleInput()
                );
            });
        }

        [Test]
        public void MaxIdentifiersPerMessage_IsValidated()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var _ = new ExtractionMessageSender(
                    new ExtractImagesOptions { MaxIdentifiersPerMessage = 0 },
                    new ExtractImagesCliOptions(),
                    new TestProducer<ExtractionRequestMessage>(),
                    new TestProducer<ExtractionRequestInfoMessage>(),
                    new FileSystem(),
                    "extractRoot",
                    "extractDir",
                    new TestDateTimeProvider(),
                    new RealConsoleInput()
                );
            });
        }


        [Test]
        public void IdList_IsNotEmpty()
        {
            var sender = new ExtractionMessageSender(
                new ExtractImagesOptions(),
                new ExtractImagesCliOptions { ProjectId = "1234-5678" },
                new TestProducer<ExtractionRequestMessage>(),
                new TestProducer<ExtractionRequestInfoMessage>(),
                new FileSystem(),
                    "extractRoot",
                "extractDir",
                new TestDateTimeProvider(),
                new RealConsoleInput()
            );

            var exc = Assert.Throws<ArgumentException>(() =>
            {
                sender.SendMessages(ExtractionKey.StudyInstanceUID, []);
            });
            Assert.That(exc?.Message, Is.EqualTo("ID list is empty"));
        }

        [Test]
        public void ListChunking_CorrectIds()
        {
            var mockExtractionRequestProducer = new TestProducer<ExtractionRequestMessage>();
            var mockExtractionRequestInfoProducer = new TestProducer<ExtractionRequestInfoMessage>();

            var processor = new ExtractionMessageSender(
                new ExtractImagesOptions { MaxIdentifiersPerMessage = 1 },
                new ExtractImagesCliOptions { ProjectId = "1234-5678", NonInteractive = true },
                mockExtractionRequestProducer,
                mockExtractionRequestInfoProducer,
                new FileSystem(),
                "extractRoot",
                "extractDir",
                new TestDateTimeProvider(),
                new RealConsoleInput()
            );

            var idList = Enumerable.Range(0, 5).Select(static x => x.ToString()).ToList();
            processor.SendMessages(ExtractionKey.StudyInstanceUID, idList);

            Assert.Multiple(() =>
            {
                Assert.That(mockExtractionRequestProducer.TotalSent, Is.EqualTo(5));
                Assert.That(mockExtractionRequestInfoProducer.TotalSent, Is.EqualTo(1));
                Assert.That(idList.SequenceEqual(mockExtractionRequestProducer.Bodies.SelectMany(static x => x.ExtractionIdentifiers)), Is.True);
            });
        }

        [TestCase(1, 1, 1)] // nIds = maxPerMessage  => 1 message
        [TestCase(1, 10, 1)] // nIds < maxPerMessage => 1 message
        [TestCase(2, 1, 2)] // nIds > maxPerMessage => 2 messages
        public void ListChunking_EdgeCases(int nIds, int maxPerMessage, int expectedMessages)
        {
            var mockExtractionRequestProducer = new TestProducer<ExtractionRequestMessage>();
            var mockExtractionRequestInfoProducer = new TestProducer<ExtractionRequestInfoMessage>();

            var processor = new ExtractionMessageSender(
                new ExtractImagesOptions { MaxIdentifiersPerMessage = maxPerMessage },
                new ExtractImagesCliOptions { ProjectId = "1234-5678", NonInteractive = true },
                mockExtractionRequestProducer,
                mockExtractionRequestInfoProducer,
                new FileSystem(),
                "extractRoot",
                "extractDir",
                new TestDateTimeProvider(),
                new RealConsoleInput()
            );

            var idList = Enumerable.Range(0, nIds).Select(static x => x.ToString()).ToList();
            processor.SendMessages(ExtractionKey.StudyInstanceUID, idList);

            Assert.Multiple(() =>
            {
                Assert.That(mockExtractionRequestProducer.TotalSent, Is.EqualTo(expectedMessages));
                Assert.That(mockExtractionRequestInfoProducer.TotalSent, Is.EqualTo(1));
            });
        }

        #endregion
    }
}
