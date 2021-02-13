using Microservices.FileCopier.Execution;
using Newtonsoft.Json;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Smi.Common.Messages.Extraction;
using Smi.Common.Options;
using Smi.Common.Tests;
using System;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Text;


namespace Microservices.FileCopier.Tests.Execution
{
    [RequiresRabbit]
    public class FileCopierHostTest
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

        #endregion

        #region Tests

        [Test]
        public void Test_FileCopierHost_HappyPath()
        {
            GlobalOptions globals = new GlobalOptionsFactory().Load();
            globals.FileSystemOptions.FileSystemRoot = "root";
            globals.FileSystemOptions.ExtractRoot = "exroot";

            using var tester = new MicroserviceTester(globals.RabbitOptions, globals.FileCopierOptions);

            string outputQueueName = globals.FileCopierOptions.CopyStatusProducerOptions.ExchangeName.Replace("Exchange", "Queue");
            tester.CreateExchange(
                globals.FileCopierOptions.CopyStatusProducerOptions.ExchangeName,
                outputQueueName,
                false,
                globals.FileCopierOptions.NoVerifyRoutingKey);

            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddDirectory(globals.FileSystemOptions.FileSystemRoot);
            mockFileSystem.AddDirectory(globals.FileSystemOptions.ExtractRoot);
            mockFileSystem.AddFile(mockFileSystem.Path.Combine(globals.FileSystemOptions.FileSystemRoot, "file.dcm"), MockFileData.NullObject);

            var host = new FileCopierHost(globals, mockFileSystem);
            tester.StopOnDispose.Add(host);
            host.Start();

            var message = new ExtractFileMessage
            {
                ExtractionJobIdentifier = Guid.NewGuid(),
                JobSubmittedAt = DateTime.UtcNow,
                ProjectNumber = "1234",
                ExtractionDirectory = "1234/foo",
                DicomFilePath = "file.dcm",
                IsIdentifiableExtraction = true,
                OutputPath = "output.dcm",
            };
            tester.SendMessage(globals.FileCopierOptions, message);

            using IConnection conn = tester.Factory.CreateConnection();
            using IModel model = conn.CreateModel();
            var consumer = new EventingBasicConsumer(model);
            ExtractedFileStatusMessage statusMessage = null;
            consumer.Received += (_, ea) => statusMessage = JsonConvert.DeserializeObject<ExtractedFileStatusMessage>(Encoding.UTF8.GetString(ea.Body.ToArray()));
            model.BasicConsume(outputQueueName, true, "", consumer);

            new TestTimelineAwaiter().Await(() => statusMessage != null);
            Assert.AreEqual(ExtractedFileStatus.Copied, statusMessage.Status);
        }

        #endregion
    }
}
