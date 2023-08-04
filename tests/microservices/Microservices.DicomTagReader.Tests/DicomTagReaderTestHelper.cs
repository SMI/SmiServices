
using Moq;
using NLog;
using RabbitMQ.Client;
using Smi.Common.Execution;
using Smi.Common.Messages;
using Smi.Common.Messaging;
using Smi.Common.Options;
using Smi.Common.Tests;
using System.IO;
using System.IO.Abstractions.TestingHelpers;


namespace Microservices.DicomTagReader.Tests
{
    public class DicomTagReaderTestHelper
    {
        private const string TestSeriesQueueName = "TEST.SeriesQueue";
        private const string TestImageQueueName = "TEST.ImageQueue";

        public readonly ILogger MockLogger = Mock.Of<ILogger>();

        public ConsumerOptions AccessionConsumerOptions = null!;

        public AccessionDirectoryMessage TestAccessionDirectoryMessage = null!;

        private IConnection _testConnection = null!;
        private IModel _testModel = null!;

        public Mock<IProducerModel> TestSeriesModel = null!;
        public Mock<IProducerModel> TestImageModel = null!;

        public MockFileSystem MockFileSystem = null!;
        public IMicroserviceHost MockHost = null!;

        public DirectoryInfo TestDir = null!;
        public GlobalOptions Options = null!;

        /// <summary>
        /// Returns the number of image messages in <see cref="TestImageQueueName"/>
        /// </summary>
        public uint ImageCount => _testModel.MessageCount(TestImageQueueName);

        /// <summary>
        /// Returns the number of series messages in <see cref="TestSeriesQueueName"/>
        /// </summary>
        public uint SeriesCount => _testModel.MessageCount(TestSeriesQueueName);


        public void SetUpSuite()
        {
            SetUpDefaults();

            // Create the test Series/Image exchanges
            var tester = new MicroserviceTester(Options.RabbitOptions);
            tester.CreateExchange(Options.DicomTagReaderOptions.ImageProducerOptions.ExchangeName,  TestImageQueueName);
            tester.CreateExchange(Options.DicomTagReaderOptions.SeriesProducerOptions.ExchangeName, TestSeriesQueueName);
            tester.CreateExchange(Options.RabbitOptions.FatalLoggingExchange, null);
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

            _testModel.QueuePurge(TestSeriesQueueName);
            _testModel.QueuePurge(TestImageQueueName);
        }

        private void SetUpDefaults()
        {
            Options = new GlobalOptionsFactory().Load(nameof(DicomTagReaderTestHelper));

            AccessionConsumerOptions = Options.DicomTagReaderOptions;

            new ProducerOptions
            {
                ExchangeName = Options.RabbitOptions.FatalLoggingExchange
            };

            TestAccessionDirectoryMessage = new AccessionDirectoryMessage
            {
                DirectoryPath = @"C:\Temp\",
            };

            TestSeriesModel = new Mock<IProducerModel>();
            TestImageModel = new Mock<IProducerModel>();

            MockFileSystem = new MockFileSystem();
            MockFileSystem.AddDirectory(@"C:\Temp");

            TestDir = new DirectoryInfo("DicomTagReaderTests");
            TestDir.Create();

            foreach (FileInfo f in TestDir.GetFiles())
                f.Delete();

            TestData.Create(new FileInfo(Path.Combine(TestDir.FullName, "MyTestFile.dcm")));
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
