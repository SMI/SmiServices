using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq.Expressions;
using System.Threading;
using Moq;
using NUnit.Framework;
using Rdmp.Core.Curation.Data;
using ReusableLibraryCode.Checks;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.Options;
using Smi.Common.Tests;
using SmiPlugin;
using Tests.Common;

namespace Applications.ExtractImages.Tests
{
    [RequiresRabbit]
    public class SmiImageExtractorTests : UnitTests
    {
        #region Fixture Methods

        [OneTimeSetUp]
        protected override void OneTimeSetUp()
        {
            base.OneTimeSetUp();

            TestLogger.Setup();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() { }

        #endregion

        #region Test Methods

        [TearDown]
        public void TearDown() { }

        #endregion

        #region Tests

        [Test]
        public void SmiImageExtractor_CheckConnectionSettings()
        {
            GlobalOptions globals = new GlobalOptionsFactory().Load(nameof(SmiImageExtractor_CheckConnectionSettings));
            globals.ExtractImagesOptions.MaxIdentifiersPerMessage = 1;

            var creds = WhenIHaveA<DataAccessCredentials>();
            creds.Username = globals.RabbitOptions.RabbitMqUserName;
            creds.Password = globals.RabbitOptions.RabbitMqPassword;

            var extractor = new SmiImageExtractor
            {
                RabbitMqCredentials = creds,
                RabbitMqHostName = globals.RabbitOptions.RabbitMqHostName,
                RabbitMqHostPort = globals.RabbitOptions.RabbitMqHostPort,
                RabbitMqVirtualHost = globals.RabbitOptions.RabbitMqVirtualHost,
                ExtractFilesExchange = globals.CohortExtractorOptions.ExtractFilesProducerOptions.ExchangeName,
                ExtractFilesInfoExchange = globals.CohortExtractorOptions.ExtractFilesInfoProducerOptions.ExchangeName,
                ImageExtractionSubDirectory = "someproj/",
            };

            using var tester = new MicroserviceTester(globals.RabbitOptions);
            tester.CreateExchange(globals.CohortExtractorOptions.ExtractFilesProducerOptions.ExchangeName);
            tester.CreateExchange(globals.CohortExtractorOptions.ExtractFilesInfoProducerOptions.ExchangeName);

            extractor.Check(new ThrowImmediatelyCheckNotifier());
        }
        #endregion
    }
}
