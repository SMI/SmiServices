using Newtonsoft.Json;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SmiServices.Common.Messages.Extraction;
using SmiServices.Common.Options;
using SmiServices.Microservices.FileCopier;
using SmiServices.UnitTests.Common;
using SmiServices.UnitTests.TestCommon;
using System;
using System.IO.Abstractions.TestingHelpers;
using System.Text;

namespace SmiServices.IntegrationTests.Microservices.FileCopier
{
    [RequiresRabbit]
    public class FileCopierHostTest
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

        #endregion

        #region Tests

        [Test]
        public void Test_FileCopierHost_HappyPath()
        {
            var globals = new GlobalOptionsFactory().Load(nameof(Test_FileCopierHost_HappyPath));
            globals.FileSystemOptions!.FileSystemRoot = "root";
            globals.FileSystemOptions.ExtractRoot = "exroot";

            using var tester = new MicroserviceTester(globals.RabbitOptions!, globals.FileCopierOptions!);

            var outputQueueName = globals.FileCopierOptions!.CopyStatusProducerOptions!.ExchangeName!.Replace("Exchange", "Queue");
            tester.CreateExchange(
                globals.FileCopierOptions.CopyStatusProducerOptions.ExchangeName,
                outputQueueName,
                false,
                globals.FileCopierOptions.NoVerifyRoutingKey!);

            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddDirectory(globals.FileSystemOptions.FileSystemRoot);
            mockFileSystem.AddDirectory(globals.FileSystemOptions.ExtractRoot);
            mockFileSystem.AddFile(mockFileSystem.Path.Combine(globals.FileSystemOptions.FileSystemRoot, "file.dcm"), null);

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

            using var model = tester.Broker.GetModel(nameof(FileCopierHostTest));
            var consumer = new EventingBasicConsumer(model);
            ExtractedFileStatusMessage? statusMessage = null;
            consumer.Received += (_, ea) => statusMessage = JsonConvert.DeserializeObject<ExtractedFileStatusMessage>(Encoding.UTF8.GetString(ea.Body.ToArray()));
            model.BasicConsume(outputQueueName, true, "", consumer);

            TestTimelineAwaiter.Await(() => statusMessage != null);
            Assert.That(statusMessage!.Status, Is.EqualTo(ExtractedFileStatus.Copied));
        }

        #endregion
    }
}
