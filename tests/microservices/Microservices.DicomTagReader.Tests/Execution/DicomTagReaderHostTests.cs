

using BadMedicine.Dicom;
using Microservices.DicomTagReader.Execution;
using NUnit.Framework;
using Smi.Common.Messages;
using Smi.Common.Tests;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;


namespace Microservices.DicomTagReader.Tests.Execution
{
    [TestFixture, RequiresRabbit]
    public class DicomTagReaderHostTests
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
        public void TearDown()
        {

        }


        /// <summary>
        /// Tests basic operation of the tag reader when receiving a single message
        /// </summary>
        [Test]
        public void TestBasicOperation()
        {
            _helper.Options.FileSystemOptions!.FileSystemRoot = _helper.TestDir.FullName;
            _helper.TestAccessionDirectoryMessage.DirectoryPath = _helper.TestDir.FullName;

            var tester = new MicroserviceTester(_helper.Options.RabbitOptions!, _helper.AccessionConsumerOptions);

            var host = new DicomTagReaderHost(_helper.Options);
            host.Start();

            tester.SendMessage(_helper.AccessionConsumerOptions, new MessageHeader(), _helper.TestAccessionDirectoryMessage);

            var timeout = 30000;
            const int stepSize = 500;

            while (!_helper.CheckQueues(1, 1) && timeout > 0)
            {
                timeout -= 500;
                Thread.Sleep(stepSize);
            }

            host.Stop("Test end");
            tester.Dispose();

            if (timeout <= 0)
                Assert.Fail("Failed to process expected number of messages within the timeout");
        }

        [Test]
        public void TestTagReader_SingleFileMode()
        {
            var dirRoot = new DirectoryInfo(Path.Combine(TestContext.CurrentContext.WorkDirectory, "TestTagReader_SingleFileMode"));

            if (dirRoot.Exists)
                dirRoot.Delete(true);

            dirRoot.Create();
            var julyFolder = dirRoot.CreateSubdirectory("July");

            _helper.Options.FileSystemOptions!.FileSystemRoot = dirRoot.FullName;

            new MicroserviceTester(_helper.Options.RabbitOptions!, _helper.AccessionConsumerOptions);

            var host = new DicomTagReaderHost(_helper.Options);

            var r = new Random(5);
            var generator = new DicomDataGenerator(r, julyFolder.FullName, "CT");
            var files = generator.GenerateImageFiles(10, r).ToArray();

            host.AccessionDirectoryMessageConsumer.RunSingleFile(files[2]);

            Assert.Multiple(() =>
            {
                Assert.That(_helper.ImageCount, Is.EqualTo(1));
                Assert.That(_helper.SeriesCount, Is.EqualTo(1));
            });

            var julyZip = Path.Combine(dirRoot.FullName, "july.zip");

            ZipFile.CreateFromDirectory(julyFolder.FullName, julyZip);

            host.AccessionDirectoryMessageConsumer.RunSingleFile(new FileInfo(julyZip));

            Assert.Multiple(() =>
            {
                Assert.That(_helper.ImageCount, Is.EqualTo(11));
                Assert.That(_helper.SeriesCount, Is.GreaterThanOrEqualTo(1));
            });
        }
    }
}
