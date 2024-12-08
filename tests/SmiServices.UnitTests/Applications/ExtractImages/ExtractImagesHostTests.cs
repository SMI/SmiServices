using Moq;
using NUnit.Framework;
using SmiServices.Applications.ExtractImages;
using SmiServices.Common;
using SmiServices.Common.Options;
using System;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace SmiServices.UnitTests.Applications.ExtractImages
{
    internal class ExtractImagesHostTests
    {
        [Test]
        public void Constructor_HappyPathWithoutPool()
        {
            var globals = new GlobalOptionsFactory().Load(nameof(Constructor_HappyPathWithoutPool));
            globals.FileSystemOptions!.ExtractRoot = "extract-root";
            globals.FileSystemOptions.ExtractionPoolRoot = null;

            var cliOptions = new ExtractImagesCliOptions
            {
                CohortCsvFile = "foo.csv",
            };

            var fileSystem = new MockFileSystem();
            fileSystem.Directory.CreateDirectory("extract-root");
            fileSystem.File.Create("foo.csv");

            var mockMessageBroker = new Mock<IMessageBroker>(MockBehavior.Strict);
            var mockSender = new Mock<IExtractionMessageSender>(MockBehavior.Strict);

            var host = new ExtractImagesHost(globals, cliOptions, mockSender.Object, mockMessageBroker.Object, fileSystem, false);
        }

        [Test]
        public void Constructor_HappyPathWithPool()
        {
            var globals = new GlobalOptionsFactory().Load(nameof(Constructor_HappyPathWithPool));
            globals.FileSystemOptions!.ExtractRoot = "extract-root";
            globals.FileSystemOptions.ExtractionPoolRoot = "pool";

            var cliOptions = new ExtractImagesCliOptions
            {
                CohortCsvFile = "foo.csv",
            };

            var fileSystem = new MockFileSystem();
            fileSystem.Directory.CreateDirectory("extract-root");
            fileSystem.File.Create("foo.csv");
            fileSystem.Directory.CreateDirectory("pool");

            var mockMessageBroker = new Mock<IMessageBroker>(MockBehavior.Strict);
            var mockSender = new Mock<IExtractionMessageSender>(MockBehavior.Strict);

            var host = new ExtractImagesHost(globals, cliOptions, mockSender.Object, mockMessageBroker.Object, fileSystem, false);
        }

        [TestCase(null)]
        [TestCase("some/missing/path")]
        public void Constructor_PooledExtractionWithNoRootSet_ThrowsException(string? poolRoot)
        {
            // Arrange

            var globals = new GlobalOptionsFactory().Load(nameof(Constructor_PooledExtractionWithNoRootSet_ThrowsException));
            globals.FileSystemOptions!.ExtractRoot = "extract-root";
            globals.FileSystemOptions.ExtractionPoolRoot = poolRoot;

            var cliOptions = new ExtractImagesCliOptions
            {
                CohortCsvFile = "foo.csv",
                IsPooledExtraction = true,
            };

            var fileSystem = new MockFileSystem();
            fileSystem.Directory.CreateDirectory("extract-root");

            var mockMessageBroker = new Mock<IMessageBroker>(MockBehavior.Strict);
            var mockSender = new Mock<IExtractionMessageSender>(MockBehavior.Strict);

            // Act

            void act() => _ = new ExtractImagesHost(globals, cliOptions, mockSender.Object, mockMessageBroker.Object, fileSystem, false);

            // Assert

            var exc = Assert.Throws<InvalidOperationException>(act);
            Assert.That(exc.Message, Is.EqualTo("IsPooledExtraction can only be passed if ExtractionPoolRoot is a directory"));
        }

        [Test]
        public void Constructor_PooledExtractionWithIsIdentifiableExtractionSet_ThrowsException()
        {
            // Arrange

            var globals = new GlobalOptionsFactory().Load(nameof(Constructor_PooledExtractionWithIsIdentifiableExtractionSet_ThrowsException));
            globals.FileSystemOptions!.ExtractRoot = "extract-root";
            globals.FileSystemOptions.ExtractionPoolRoot = "pool";

            var cliOptions = new ExtractImagesCliOptions
            {
                CohortCsvFile = "foo.csv",
                IsPooledExtraction = true,
                IsIdentifiableExtraction = true,
            };

            var fileSystem = new MockFileSystem();
            fileSystem.Directory.CreateDirectory("extract-root");
            fileSystem.Directory.CreateDirectory("pool");

            var mockMessageBroker = new Mock<IMessageBroker>(MockBehavior.Strict);
            var mockSender = new Mock<IExtractionMessageSender>(MockBehavior.Strict);

            // Act

            void act() => _ = new ExtractImagesHost(globals, cliOptions, mockSender.Object, mockMessageBroker.Object, fileSystem, false);

            // Assert

            var exc = Assert.Throws<InvalidOperationException>(act);
            Assert.That(exc.Message, Is.EqualTo("IsPooledExtraction is incompatible with IsIdentifiableExtraction"));
        }

        [Test]
        public void Constructor_PooledExtractionWithIsNoFilterExtractionSet_ThrowsException()
        {
            // Arrange

            var globals = new GlobalOptionsFactory().Load(nameof(Constructor_PooledExtractionWithIsNoFilterExtractionSet_ThrowsException));
            globals.FileSystemOptions!.ExtractRoot = "extract-root";
            globals.FileSystemOptions.ExtractionPoolRoot = "pool";

            var cliOptions = new ExtractImagesCliOptions
            {
                CohortCsvFile = "foo.csv",
                IsPooledExtraction = true,
                IsNoFiltersExtraction = true,
            };

            var fileSystem = new MockFileSystem();
            fileSystem.Directory.CreateDirectory("extract-root");
            fileSystem.Directory.CreateDirectory("pool");

            var mockMessageBroker = new Mock<IMessageBroker>(MockBehavior.Strict);
            var mockSender = new Mock<IExtractionMessageSender>(MockBehavior.Strict);

            // Act

            void act() => _ = new ExtractImagesHost(globals, cliOptions, mockSender.Object, mockMessageBroker.Object, fileSystem, false);

            // Assert

            var exc = Assert.Throws<InvalidOperationException>(act);
            Assert.That(exc.Message, Is.EqualTo("IsPooledExtraction is incompatible with IsNoFiltersExtraction"));
        }
    }
}
