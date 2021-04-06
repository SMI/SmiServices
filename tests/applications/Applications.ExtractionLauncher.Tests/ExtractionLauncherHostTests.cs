using Moq;
using NUnit.Framework;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.Options;
using Smi.Common.Tests;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq.Expressions;
using System.Threading;


namespace Applications.ExtractionLauncher.Tests
{
    [RequiresRabbit]
    public class ExtractionLauncherHostTests
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
        public void HappyPath()
        {
            GlobalOptions globals = new GlobalOptionsFactory().Load(nameof(HappyPath));
            globals.ExtractionLauncherOptions.MaxIdentifiersPerMessage = 1;

            var cliOptions = new ExtractionLauncherCliOptions { CohortCsvFile = "foo.csv", ProjectId = "1234-5678", NonInteractive = true };

            var fs = new MockFileSystem(
                new Dictionary<string, MockFileData>
                {
                    {"foo.csv", "SeriesInstanceUID\n1.2.3.4"},
                }
            );

            Expression<Action<IExtractionMessageSender>> expr = x => x.SendMessages(ExtractionKey.SeriesInstanceUID, new List<string> { "1.2.3.4" });
            var mockExtractionMessageSender = new Mock<IExtractionMessageSender>(MockBehavior.Strict);
            mockExtractionMessageSender.Setup(expr);

            var host = new ExtractionLauncherHost(globals, cliOptions, mockExtractionMessageSender.Object, fileSystem: fs);
            host.Start();

            mockExtractionMessageSender.Verify(expr, Times.Once);
        }

        [Test]
        public void HappyPath_Integration()
        {
            GlobalOptions globals = new GlobalOptionsFactory().Load(nameof(HappyPath_Integration));

            string extractRoot = Path.GetTempPath();
            globals.FileSystemOptions.ExtractRoot = extractRoot;

            ExtractionLauncherOptions options = globals.ExtractionLauncherOptions;

            string tmpFile = Path.GetTempFileName();
            File.WriteAllText(tmpFile, "SeriesInstanceUID\n1.2.3.4");

            var cliOptions = new ExtractionLauncherCliOptions
            {
                CohortCsvFile = tmpFile,
                ProjectId = "1234-5678",
                NonInteractive = true,
                Modalities = "CT,MR",
                IsIdentifiableExtraction = true,
                IsNoFiltersExtraction = true,
            };

            var extReqExchName = options.ExtractionRequestProducerOptions.ExchangeName;
            var extReqInfoExchName = options.ExtractionRequestInfoProducerOptions.ExchangeName;

            var consumedExtReqMsgs = new List<Tuple<IMessageHeader, ExtractionRequestMessage>>();
            var consumedExtReqInfoMsgs = new List<Tuple<IMessageHeader, ExtractionRequestInfoMessage>>();

            using (var tester = new MicroserviceTester(globals.RabbitOptions))
            {
                tester.CreateExchange(extReqExchName);
                tester.CreateExchange(extReqInfoExchName);

                var host = new ExtractionLauncherHost(globals, cliOptions);
                host.Start();

                var timeoutSecs = 10.0;
                const double delta = 0.1;
                while ((consumedExtReqMsgs.Count == 0 || consumedExtReqInfoMsgs.Count == 0) && timeoutSecs >= 0)
                {
                    consumedExtReqMsgs.AddRange(tester.ConsumeMessages<ExtractionRequestMessage>(extReqExchName.Replace("Exchange", "Queue")));
                    consumedExtReqInfoMsgs.AddRange(tester.ConsumeMessages<ExtractionRequestInfoMessage>(extReqInfoExchName.Replace("Exchange", "Queue")));

                    timeoutSecs -= delta;
                    Thread.Sleep(TimeSpan.FromSeconds(delta));
                }

                Assert.True(timeoutSecs > 0);
            }

            File.Delete(tmpFile);

            Assert.AreEqual(1, consumedExtReqMsgs.Count);
            ExtractionRequestMessage receivedRequestMessage = consumedExtReqMsgs[0].Item2;
            Assert.AreEqual("SeriesInstanceUID", receivedRequestMessage.KeyTag);
            Assert.AreEqual("CT,MR", receivedRequestMessage.Modalities);
            Assert.AreEqual(new List<string> { "1.2.3.4" }, receivedRequestMessage.ExtractionIdentifiers);

            Assert.AreEqual(1, consumedExtReqInfoMsgs.Count);
            ExtractionRequestInfoMessage receivedRequestInfoMessage = consumedExtReqInfoMsgs[0].Item2;
            Assert.AreEqual("SeriesInstanceUID", receivedRequestInfoMessage.KeyTag);
            Assert.AreEqual("CT,MR", receivedRequestInfoMessage.ExtractionModality);
            Assert.AreEqual(1, receivedRequestInfoMessage.KeyValueCount);

            foreach (IExtractMessage msg in new List<IExtractMessage> { receivedRequestMessage, receivedRequestInfoMessage })
            {
                Assert.AreEqual("1234-5678", msg.ProjectNumber);
                Assert.AreEqual(Path.Join("1234-5678", "extractions", Path.GetFileNameWithoutExtension(tmpFile)), msg.ExtractionDirectory);
                Assert.True(msg.IsIdentifiableExtraction);
                Assert.True(msg.IsNoFilterExtraction);
            }
        }

        [Test]
        public void ExtractionLauncherOptions_AreValid()
        {
            GlobalOptions globals = new GlobalOptionsFactory().Load(nameof(ExtractionLauncherOptions_AreValid));
            globals.ExtractionLauncherOptions = null;

            var exc = Assert.Throws<ArgumentException>(() =>
            {
                var _ = new ExtractionLauncherHost(globals, new ExtractionLauncherCliOptions());
            });
            Assert.AreEqual("ExtractionLauncherOptions", exc?.Message);
        }

        [Test]
        public void ExtractionRoot_VerifyPresent()
        {
            GlobalOptions globals = new GlobalOptionsFactory().Load(nameof(ExtractionRoot_VerifyPresent));
            globals.FileSystemOptions.ExtractRoot = "nope";

            var exc = Assert.Throws<DirectoryNotFoundException>(() =>
            {
                var _ = new ExtractionLauncherHost(globals, new ExtractionLauncherCliOptions());
            });
            Assert.AreEqual("Could not find the extraction root 'nope'", exc?.Message);
        }

        [Test]
        public void CsvFile_VerifyPresent()
        {
            GlobalOptions globals = new GlobalOptionsFactory().Load(nameof(CsvFile_VerifyPresent));
            globals.FileSystemOptions.ExtractRoot = "extract-root";

            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(globals.FileSystemOptions.ExtractRoot);

            var cliOptions = new ExtractionLauncherCliOptions { CohortCsvFile = null };
            var exc = Assert.Throws<ArgumentNullException>(() =>
            {
                var _ = new ExtractionLauncherHost(globals, cliOptions, fileSystem: fs);
            });
            Assert.AreEqual("Value cannot be null. (Parameter 'CohortCsvFile')", exc?.Message);

            cliOptions.CohortCsvFile = "missing.csv";
            var exc2 = Assert.Throws<FileNotFoundException>(() =>
            {
                var _ = new ExtractionLauncherHost(globals, cliOptions, fileSystem: fs);
            });
            Assert.AreEqual("Could not find the cohort CSV file 'missing.csv'", exc2?.Message);
        }

        [Test]
        public void ExtractionDirectory_VerifyAbsent()
        {
            GlobalOptions globals = new GlobalOptionsFactory().Load(nameof(ExtractionDirectory_VerifyAbsent));
            globals.FileSystemOptions.ExtractRoot = "extract-root";

            var cliOptions = new ExtractionLauncherCliOptions { CohortCsvFile = "test.csv", ProjectId = "foo" };

            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(globals.FileSystemOptions.ExtractRoot);
            fs.File.Create("test.csv", 0);
            fs.Directory.CreateDirectory("extract-root/foo/extractions/test");

            var exc = Assert.Throws<DirectoryNotFoundException>(() =>
            {
                var _ = new ExtractionLauncherHost(globals, cliOptions, fileSystem: fs);
            });
            Assert.True(exc?.Message.StartsWith("Extraction directory already exists"));
        }

        #endregion
    }
}
