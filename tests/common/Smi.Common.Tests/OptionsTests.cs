
using NUnit.Framework;
using Smi.Common.Options;
using System;
using System.Collections.Generic;

namespace Smi.Common.Tests
{
    //TODO: Rework these tests. We should assert that every option in GlobalOptions has an entry in default.yaml. Non-required options should be present with a comment
    [TestFixture]
    public class OptionsTests
    {
        [TestCase]
        public void GlobalOptions_Test()
        {
            GlobalOptions globals = new GlobalOptionsFactory().Load(nameof(GlobalOptions_Test));
            Assert.IsFalse(string.IsNullOrWhiteSpace(globals.RabbitOptions!.RabbitMqHostName));
            Assert.IsFalse(string.IsNullOrWhiteSpace(globals.FileSystemOptions!.FileSystemRoot));
            Assert.IsFalse(string.IsNullOrWhiteSpace(globals.RDMPOptions!.CatalogueConnectionString));
            Assert.IsFalse(string.IsNullOrWhiteSpace(globals.RDMPOptions.DataExportConnectionString));
        }

        [Test]
        public void TestVerifyPopulatedChecks()
        {
            var producerOptions = new ProducerOptions();

            Assert.False(producerOptions.VerifyPopulated());

            producerOptions.ExchangeName = "";
            Assert.False(producerOptions.VerifyPopulated());

            producerOptions.ExchangeName = "Test.ExchangeName";
            Assert.True(producerOptions.VerifyPopulated());

            var consumerOptions = new ConsumerOptions();

            Assert.False(consumerOptions.VerifyPopulated());

            consumerOptions.QueueName = "Test.QueueName";
            Assert.False(consumerOptions.VerifyPopulated());

            consumerOptions.QoSPrefetchCount = 1234;
            Assert.True(consumerOptions.VerifyPopulated());
        }

        [Test]
        public void Test_GlobalOptionsUseTestValues_Nulls()
        {
            GlobalOptions g = new GlobalOptionsFactory().Load(nameof(Test_GlobalOptionsUseTestValues_Nulls));

            Assert.IsNotNull(g.RabbitOptions!.RabbitMqHostName);
            g.UseTestValues(null, null, null, null, null);
            Assert.IsNull(g.RabbitOptions.RabbitMqHostName);
        }

        [Test]
        public void Test_GlobalOptions_FileReadOption_ThrowsException()
        {
            GlobalOptions g = new GlobalOptionsFactory().Load(nameof(Test_GlobalOptions_FileReadOption_ThrowsException));
            g.DicomTagReaderOptions!.FileReadOption = "SkipLargeTags";

            Assert.Throws<ApplicationException>(() => g.DicomTagReaderOptions.GetReadOption());
        }


        private class TestDecorator : OptionsDecorator
        {
            public override GlobalOptions Decorate(GlobalOptions options)
            {
                ForAll<MongoDbOptions>(options, (o) => new MongoDbOptions { DatabaseName = "FFFFF" });
                return options;
            }
        }

        [Test]
        public void TestDecorators()
        {
            var factory = new GlobalOptionsFactory(new List<IOptionsDecorator> { new TestDecorator() });
            var g = factory.Load(nameof(TestDecorators));
            Assert.AreEqual("FFFFF", g.MongoDatabases!.DicomStoreOptions!.DatabaseName);
            Assert.AreEqual("FFFFF", g.MongoDatabases.ExtractionStoreOptions!.DatabaseName);
        }
    }
}
