
using NUnit.Framework;
using Smi.Common.Options;

namespace Smi.Common.Tests
{
    //TODO: Rework these tests. We should assert that every option in GlobalOptions has an entry in default.yaml. Non-required options should be present with a comment
    [TestFixture]
    public class OptionsTests
    {
        [TestCase("default.yaml")]
        public void GlobalOptions_Test(string template)
        {
            GlobalOptions globals = GlobalOptions.Load(template, TestContext.CurrentContext.TestDirectory);
            Assert.IsFalse(string.IsNullOrWhiteSpace(globals.RabbitOptions.RabbitMqHostName));
            Assert.IsFalse(string.IsNullOrWhiteSpace(globals.FileSystemOptions.FileSystemRoot));
            Assert.IsFalse(string.IsNullOrWhiteSpace(globals.RDMPOptions.CatalogueConnectionString));
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
    }
}
