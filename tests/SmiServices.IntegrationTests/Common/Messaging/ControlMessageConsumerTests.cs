using NUnit.Framework;
using SmiServices.Common.Messaging;
using SmiServices.Common.Options;

namespace SmiServices.IntegrationTests.Common.Messaging;

[RequiresRabbit]
internal class ControlMessageConsumerTests
{
    private static GlobalOptions GlobalOptionsForTest()
        => new GlobalOptionsFactory().Load(TestContext.CurrentContext.Test.Name);

    [Test]
    public void ProcessMessage_HappyPath_IsOk()
    {
        // Arrange

        var globalOptions = GlobalOptionsForTest();
        var consumer = new ControlMessageConsumer(
            globalOptions.RabbitOptions!,
            TestContext.CurrentContext.Test.Name,
            123,
            globalOptions.RabbitOptions!.RabbitMqControlExchangeName!,
            (_) => { }
        );

        string? routingKey = null;
        string? message = null;
        consumer.ControlEvent += (string r, string? m) => { routingKey = r; message = m; };

        // Act

        consumer.ProcessMessage("foo", $"smi.control.{TestContext.CurrentContext.Test.Name.ToLower()}.test");

        // Assert

        Assert.Multiple(() =>
        {
            Assert.That(routingKey, Is.EqualTo("test"));
            Assert.That(message, Is.EqualTo("foo"));
        });
    }
}
