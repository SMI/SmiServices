using NUnit.Framework;
using SmiServices.Common.Options;
using System;
using System.IO.Abstractions.TestingHelpers;

namespace SmiServices.IntegrationTests.Common;

//TODO: Rework these tests. We should assert that every option in GlobalOptions has an entry in default.yaml. Non-required options should be present with a comment
[TestFixture]
public class OptionsTests
{
    [TestCase]
    public void GlobalOptions_Test()
    {
        GlobalOptions globals = new GlobalOptionsFactory().Load(nameof(GlobalOptions_Test));
        Assert.Multiple(() =>
        {
            Assert.That(string.IsNullOrWhiteSpace(globals.RabbitOptions!.RabbitMqHostName), Is.False);
            Assert.That(string.IsNullOrWhiteSpace(globals.FileSystemOptions!.FileSystemRoot), Is.False);
            Assert.That(string.IsNullOrWhiteSpace(globals.RDMPOptions!.CatalogueConnectionString), Is.False);
            Assert.That(string.IsNullOrWhiteSpace(globals.RDMPOptions.DataExportConnectionString), Is.False);
        });
    }

    [Test]
    public void TestVerifyPopulatedChecks()
    {
        var producerOptions = new ProducerOptions();

        Assert.That(producerOptions.VerifyPopulated(), Is.False);

        producerOptions.ExchangeName = "";
        Assert.That(producerOptions.VerifyPopulated(), Is.False);

        producerOptions.ExchangeName = "Test.ExchangeName";
        Assert.That(producerOptions.VerifyPopulated(), Is.True);

        var consumerOptions = new ConsumerOptions();

        Assert.That(consumerOptions.VerifyPopulated(), Is.False);

        consumerOptions.QueueName = "Test.QueueName";
        Assert.That(consumerOptions.VerifyPopulated(), Is.True);
    }

    [Test]
    public void Test_GlobalOptionsUseTestValues_Nulls()
    {
        GlobalOptions g = new GlobalOptionsFactory().Load(nameof(Test_GlobalOptionsUseTestValues_Nulls));

        Assert.That(g.RabbitOptions!.RabbitMqHostName, Is.Not.Null);
        g.UseTestValues(null, null, null, null, null);
        Assert.That(g.RabbitOptions.RabbitMqHostName, Is.Null);
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
        var factory = new GlobalOptionsFactory([new TestDecorator()]);
        var g = factory.Load(nameof(TestDecorators));
        Assert.Multiple(() =>
        {
            Assert.That(g.MongoDatabases!.DicomStoreOptions!.DatabaseName, Is.EqualTo("FFFFF"));
            Assert.That(g.MongoDatabases.ExtractionStoreOptions!.DatabaseName, Is.EqualTo("FFFFF"));
        });
    }

    [Test]
    public void GlobalOptionsFactory_Load_EmptyFile_ThrowsWithUsefulMessage()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.File.Create("foo.yaml");
        var globalOptionsFactory = new GlobalOptionsFactory();

        var exc = Assert.Throws<Exception>(() => globalOptionsFactory.Load(nameof(GlobalOptionsFactory_Load_EmptyFile_ThrowsWithUsefulMessage), "foo.yaml", fileSystem));
        Assert.That(exc?.Message, Is.EqualTo("Did not deserialize a GlobalOptions object from the provided YAML file. Does it contain at least one valid key?"));
    }

    [Test]
    public void GlobalOptionsFactory_Load_MissingFile_ThrowsWithUsefulMessage()
    {
        var fileSystem = new MockFileSystem();
        var globalOptionsFactory = new GlobalOptionsFactory();

        var exc = Assert.Throws<ArgumentException>(() => globalOptionsFactory.Load(nameof(GlobalOptionsFactory_Load_EmptyFile_ThrowsWithUsefulMessage), "foo.yaml", fileSystem));
        Assert.That(exc?.Message, Is.EqualTo("Could not find config file 'foo.yaml'"));
    }
}
