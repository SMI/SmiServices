

using Microservices.DicomTagReader.Execution;
using NUnit.Framework;
using Smi.Common.Messages;
using Smi.Common.Tests;
using System.Threading;


namespace Microservices.DicomTagReader.Tests.Execution
{
    [TestFixture, RequiresRabbit]
    public class DicomTagReaderHostTests
    {
        private readonly DicomTagReaderTestHelper _helper = new DicomTagReaderTestHelper();

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
            _helper.Options.FileSystemOptions.FileSystemRoot = _helper.TestDir.FullName;
            _helper.TestAccessionDirectoryMessage.DirectoryPath = _helper.TestDir.FullName;

            var tester = new MicroserviceTester(_helper.Options.RabbitOptions, _helper.AccessionConsumerOptions);

            var host = new DicomTagReaderHost(_helper.Options, false);
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
    }
}
