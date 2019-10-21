
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using Microservices.Common.Execution;
using Microservices.Common.Messages;
using Microservices.Common.Messaging;
using Microservices.Common.Options;
using Microservices.Common.Tests;
using Moq;
using NLog;
using NUnit.Framework;
using RabbitMQ.Client;

namespace Microservices.SMIDicomTagReader.Tests
{
    public class DicomTagReaderTestHelper
    {
        private const string TestSeriesQueueName = "TEST.SeriesQueue";
        private const string TestImageQueueName = "TEST.ImageQueue";

        public readonly ILogger MockLogger = Mock.Of<ILogger>();

        public ConsumerOptions AccessionConsumerOptions;

        public AccessionDirectoryMessage TestAccessionDirectoryMessage;

        private IConnection _testConnection;
        private IModel _testModel;

        public Mock<IProducerModel> TestSeriesModel;
        public Mock<IProducerModel> TestImageModel;

        public MockFileSystem MockFileSystem;
        public IMicroserviceHost MockHost;

        public DirectoryInfo TestDir;
        public GlobalOptions Options;

        public void SetUpSuite()
        {
            SetUpDefaults();

            // Create the test Series/Image exchanges
            Options.RabbitOptions.RabbitMqControlExchangeName = "TEST.ControlExchange";
            var tester = new MicroserviceTester(Options.RabbitOptions);
            tester.CreateExchange(Options.DicomTagReaderOptions.ImageProducerOptions.ExchangeName, new ConsumerOptions { QueueName = TestSeriesQueueName });
            tester.CreateExchange(Options.DicomTagReaderOptions.SeriesProducerOptions.ExchangeName, new ConsumerOptions { QueueName = TestImageQueueName });
            tester.CreateExchange(Options.RabbitOptions.FatalLoggingExchange, null);
            tester.CreateExchange(Options.RabbitOptions.RabbitMqControlExchangeName, null);
            tester.Shutdown();

            _testConnection = new ConnectionFactory
            {
                HostName = Options.RabbitOptions.RabbitMqHostName,
                Port = Options.RabbitOptions.RabbitMqHostPort,
                VirtualHost = Options.RabbitOptions.RabbitMqVirtualHost,
                UserName = Options.RabbitOptions.RabbitMqUserName,
                Password = Options.RabbitOptions.RabbitMqPassword
            }.CreateConnection("TestConnection");

            _testModel = _testConnection.CreateModel();

            MockHost = Mock.Of<IMicroserviceHost>();
        }

        public void ResetSuite()
        {
            SetUpDefaults();
        }

        private void SetUpDefaults()
        {
            Options = GlobalOptions.Load("default", TestContext.CurrentContext.TestDirectory);

            AccessionConsumerOptions = Options.DicomTagReaderOptions;

            new ProducerOptions
            {
                ExchangeName = Options.RabbitOptions.FatalLoggingExchange
            };

            TestAccessionDirectoryMessage = new AccessionDirectoryMessage
            {
                DirectoryPath = @"C:\Temp\",
                NationalPACSAccessionNumber = "1234"
            };

            TestSeriesModel = new Mock<IProducerModel>();
            TestImageModel = new Mock<IProducerModel>();
            
            MockFileSystem = new MockFileSystem();
            MockFileSystem.AddDirectory(@"C:\Temp");

            TestDir = new DirectoryInfo("DicomTagReaderTests");
            TestDir.Create();

            foreach (FileInfo f in TestDir.GetFiles())
                f.Delete();

            var fi = new FileInfo(Path.Combine(TestDir.FullName, "MyTestFile.dcm"));
            File.WriteAllBytes(fi.FullName, TestDicomFiles.IM_0001_0013);
        }

        public bool CheckQueues(int nInSeriesQueue, int nInImageQueue)
        {
            return
                _testModel.MessageCount(TestSeriesQueueName) == nInSeriesQueue &&
                _testModel.MessageCount(TestImageQueueName) == nInImageQueue;
        }

        public void Dispose()
        {
            _testModel.Close();
            _testConnection.Close();
        }
    }
}
