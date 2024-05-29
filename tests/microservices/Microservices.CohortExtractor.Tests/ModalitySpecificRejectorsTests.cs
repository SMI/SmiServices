using NUnit.Framework;
using Smi.Common.Options;
using System;
using System.IO;

namespace Microservices.CohortExtractor.Tests
{
    class ModalitySpecificRejectorsTests
    {

        [Test]
        public void TestDeserialization()
        {
            var factory = new GlobalOptionsFactory();
            string file;
            var yaml =
            @"
LoggingOptions:
    LogConfigFile:
CohortExtractorOptions:
    QueueName: 'TEST.RequestQueue'
    AllCatalogues: true
    RejectorType: Microservices.CohortExtractor.Execution.RequestFulfillers.RejectAll
    ModalitySpecificRejectors:
       - Modalities: CT,MR
         Overrides: true
         RejectorType: Microservices.CohortExtractor.Execution.RequestFulfillers.RejectNone
    ";

            File.WriteAllText(file = Path.Combine(TestContext.CurrentContext.WorkDirectory, "ff.yaml"), yaml);

            var opts = factory.Load("FF.DD", file);

            Assert.Multiple(() =>
            {
                Assert.That(opts.CohortExtractorOptions!.ModalitySpecificRejectors!,Has.Length.EqualTo(1));
                Assert.That(opts.CohortExtractorOptions.ModalitySpecificRejectors?[0].Modalities,Is.EqualTo("CT,MR"));
                Assert.That(opts.CohortExtractorOptions.ModalitySpecificRejectors?[0].GetModalities()[0],Is.EqualTo("CT"));
                Assert.That(opts.CohortExtractorOptions.ModalitySpecificRejectors?[0].GetModalities()[1],Is.EqualTo("MR"));
                Assert.That(opts.CohortExtractorOptions.ModalitySpecificRejectors?[0].RejectorType,Is.EqualTo("Microservices.CohortExtractor.Execution.RequestFulfillers.RejectNone"));
            });
        }

        [Test]
        public void TestValidation_MissingModalityRouting()
        {
            var factory = new GlobalOptionsFactory();
            string file;
            var yaml =
            @"
LoggingOptions:
    LogConfigFile:
CohortExtractorOptions:
    QueueName: 'TEST.RequestQueue'
    AllCatalogues: true
    RequestFulfillerType: FromCataloguesExtractionRequestFulfiller
    ModalityRoutingRegex: 
    RejectorType: Microservices.CohortExtractor.Execution.RequestFulfillers.RejectAll
    ModalitySpecificRejectors:
       - Modalities: CT,MR
         Overrides: true
         RejectorType: Microservices.CohortExtractor.Execution.RequestFulfillers.RejectNone
    ";

            File.WriteAllText(file = Path.Combine(TestContext.CurrentContext.WorkDirectory, "ff.yaml"), yaml);

            var opts = factory.Load("FF.DD", file);
            
            var ex = Assert.Throws<Exception>(()=>opts.CohortExtractorOptions!.Validate());
            Assert.That(ex!.Message,Is.EqualTo("ModalitySpecificRejectors requires providing a ModalityRoutingRegex"));
        }
    }
}
