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

            Assert.AreEqual(1, opts.CohortExtractorOptions!.ModalitySpecificRejectors!.Length);
            Assert.AreEqual("CT,MR", opts.CohortExtractorOptions.ModalitySpecificRejectors[0].Modalities);
            Assert.AreEqual("CT", opts.CohortExtractorOptions.ModalitySpecificRejectors[0].GetModalities()[0]);
            Assert.AreEqual("MR", opts.CohortExtractorOptions.ModalitySpecificRejectors[0].GetModalities()[1]);

            Assert.AreEqual("Microservices.CohortExtractor.Execution.RequestFulfillers.RejectNone", opts.CohortExtractorOptions.ModalitySpecificRejectors[0].RejectorType);
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
            Assert.AreEqual("ModalitySpecificRejectors requires providing a ModalityRoutingRegex", ex!.Message);
        }
    }
}
