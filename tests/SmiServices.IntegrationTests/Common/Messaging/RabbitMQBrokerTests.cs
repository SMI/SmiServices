using Moq;
using NUnit.Framework;
using NUnit.Framework.Internal;
using SmiServices.Common.Messages;
using SmiServices.Common.Messaging;
using SmiServices.Common.Options;
using SmiServices.UnitTests.Common;
using SmiServices.UnitTests.TestCommon;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmiServices.IntegrationTests.Common.Messaging;

[RequiresRabbit]
public class RabbitMQBrokerTests
{
    private static ConsumerOptions TestConsumerOptions()
        => new()
        {
            QueueName = "TEST.TestQueue",
            QoSPrefetchCount = 1,
            AutoAck = false
        };

    private static GlobalOptions GlobalOptionsForTest()
        => new GlobalOptionsFactory().Load(TestContext.CurrentContext.Test.Name);

    [TestCase(null)]
    [TestCase("  ")]
    public void SetupProducer_InvalidExchangeName_Throws(string? exchangeName)
    {
        // Arrange

        var producerOptions = new ProducerOptions
        {
            ExchangeName = exchangeName
        };
        var globalOptions = GlobalOptionsForTest();
        using var tester = new MicroserviceTester(globalOptions.RabbitOptions!, TestConsumerOptions());

        // Act

        void call() => tester.Broker.SetupProducer(producerOptions);

        // Assert

        var exc = Assert.Throws<ArgumentException>(call);
        Assert.That(exc.Message, Is.EqualTo("The given producer options have invalid values"));
    }

    [Test]
    public void SetupProducer_MissingExchange_Throws()
    {
        // Arrange

        var producerOptions = new ProducerOptions
        {
            ExchangeName = "TEST.DoesNotExistExchange"
        };

        var globalOptions = GlobalOptionsForTest();
        using var tester = new MicroserviceTester(globalOptions.RabbitOptions!, TestConsumerOptions());

        // Act

        void call() => tester.Broker.SetupProducer(producerOptions);

        // Assert

        var exc = Assert.Throws<ApplicationException>(call);
        Assert.That(exc.Message, Is.EqualTo("Expected exchange \"TEST.DoesNotExistExchange\" to exist"));
    }

    [Test]
    public void StartConsumer_SetsQoSPrefetchCount()
    {
        // Arrange

        var globalOptions = GlobalOptionsForTest();
        var consumerOptions = TestConsumerOptions();
        using var tester = new MicroserviceTester(globalOptions.RabbitOptions!, consumerOptions);
        var mockConsumer = new Mock<IConsumer<IMessage>>(MockBehavior.Strict);
        mockConsumer.SetupProperty(x => x.QoSPrefetchCount);
        consumerOptions.QoSPrefetchCount = 123;

        // Act

        tester.Broker.StartConsumer(consumerOptions, mockConsumer.Object);

        // Assert

        Assert.That(mockConsumer.Object.QoSPrefetchCount, Is.EqualTo(123));
    }

    [Test]
    public void StartConsumer_MissingQueue_Throws()
    {
        // Arrange

        var consumerOptions = TestConsumerOptions();
        var queueName = $"TEST.WrongQueue{new Random().NextInt64()}";
        consumerOptions.QueueName = queueName;

        var globalOptions = GlobalOptionsForTest();
        using var tester = new MicroserviceTester(globalOptions.RabbitOptions!);

        var mockConsumer = new Mock<IConsumer<IMessage>>(MockBehavior.Strict);
        mockConsumer.SetupProperty(x => x.QoSPrefetchCount);

        // Act

        void call() => tester.Broker.StartConsumer(consumerOptions, mockConsumer.Object);

        // Assert

        var exc = Assert.Throws<ApplicationException>(call);
        Assert.That(exc.Message, Is.EqualTo($"Expected queue \"{queueName}\" to exist"));
    }

    [Test]
    public void Shutdown_NoTimeout_Throws()
    {
        // Arrange

        var globalOptions = GlobalOptionsForTest();
        using var tester = new MicroserviceTester(globalOptions.RabbitOptions!, TestConsumerOptions());
        var broker = new RabbitMQBroker(globalOptions.RabbitOptions!, "RabbitMQBrokerTests");
        var mockConsumer = new Mock<IConsumer<IMessage>>(MockBehavior.Strict);
        mockConsumer.SetupProperty(x => x.QoSPrefetchCount);
        broker.StartConsumer(TestConsumerOptions(), mockConsumer.Object);

        // Act

        void call() => broker.Shutdown(TimeSpan.Zero);

        // Assert

        var exc = Assert.Throws<ApplicationException>(call);
        Assert.That(exc.Message, Is.EqualTo("Invalid timeout value"));
    }

    [Test]
    public void StartConsumer_AfterShutdown_Throws()
    {
        // Arrange

        var globalOptions = GlobalOptionsForTest();
        var broker = new RabbitMQBroker(globalOptions.RabbitOptions!, "RabbitMQBrokerTests");
        var mockConsumer = new Mock<IConsumer<IMessage>>(MockBehavior.Strict);
        mockConsumer.SetupProperty(x => x.QoSPrefetchCount);

        // Act

        broker.Shutdown(RabbitMQBroker.DefaultOperationTimeout);

        // Assert

        Assert.That(broker.ShutdownCalled, Is.EqualTo(true));
        var exc = Assert.Throws<ApplicationException>(() => broker.StartConsumer(TestConsumerOptions(), mockConsumer.Object));
        Assert.That(exc.Message, Is.EqualTo("Adapter has been shut down"));
    }

    [Test]
    public void StartConsumer_InvalidOptions_Throws()
    {
        // Arrange

        var globalOptions = GlobalOptionsForTest();
        var broker = new RabbitMQBroker(globalOptions.RabbitOptions!, "RabbitMQBrokerTests");
        var mockConsumer = new Mock<IConsumer<IMessage>>(MockBehavior.Strict);

        var consumerOptions = new ConsumerOptions()
        {
            QueueName = null,
        };

        // Act

        void call() => broker.StartConsumer(consumerOptions, mockConsumer.Object);

        // Assert

        var exc = Assert.Throws<ArgumentException>(call);
        Assert.That(exc.Message, Is.EqualTo("The given consumerOptions has invalid values"));
    }

    [Test]
    public void StartConsumer_SoloWithMultipleConsumers_Throws()
    {
        // Arrange

        var globalOptions = GlobalOptionsForTest();
        var broker = new RabbitMQBroker(globalOptions.RabbitOptions!, "RabbitMQBrokerTests");
        var mockConsumer = new Mock<IConsumer<IMessage>>(MockBehavior.Strict);
        mockConsumer.SetupProperty(x => x.QoSPrefetchCount);
        var consumerOptions = TestConsumerOptions();
        using var tester = new MicroserviceTester(globalOptions.RabbitOptions!, consumerOptions);

        broker.StartConsumer(consumerOptions, mockConsumer.Object);

        // Act

        void call() => broker.StartConsumer(consumerOptions, mockConsumer.Object, isSolo: true);

        // Assert

        var exc = Assert.Throws<ApplicationException>(call);
        Assert.That(exc.Message, Is.EqualTo("Already a consumer on queue TEST.TestQueue and solo consumer was specified"));
    }

    [Test]
    public void HandleMessage_MissingHeader_IsDiscarded()
    {
        // Arrange

        var globalOptions = GlobalOptionsForTest();
        var consumerOptions = TestConsumerOptions();
        using var tester = new MicroserviceTester(globalOptions.RabbitOptions!, consumerOptions);
        using var model = tester.Broker.GetModel(TestContext.CurrentContext.Test.Name);
        model.ConfirmSelect();
        var properties = model.CreateBasicProperties();

        var mockConsumer = new Mock<IConsumer<IMessage>>(MockBehavior.Strict);
        mockConsumer.SetupProperty(x => x.QoSPrefetchCount);
        var fatalCalled = false;
        mockConsumer.Object.OnFatal += (sender, args) => fatalCalled = true;

        var broker = new RabbitMQBroker(globalOptions.RabbitOptions!, "RabbitMQBrokerTests");
        broker.StartConsumer(consumerOptions, mockConsumer.Object);

        // Act

        model.BasicPublish("TEST.TestExchange", "", mandatory: true, properties, Array.Empty<byte>());
        model.WaitForConfirms();
        TestTimelineAwaiter.Await(() => model.MessageCount(consumerOptions.QueueName) == 0);

        // Assert

        Assert.That(fatalCalled, Is.False);
    }

    [Test]
    public void HandleMessage_InvalidMessage_IsDiscarded()
    {
        // Arrange

        var globalOptions = GlobalOptionsForTest();
        var consumerOptions = TestConsumerOptions();
        using var tester = new MicroserviceTester(globalOptions.RabbitOptions!, consumerOptions);
        using var model = tester.Broker.GetModel(TestContext.CurrentContext.Test.Name);
        model.ConfirmSelect();
        var properties = model.CreateBasicProperties();
        properties.Headers = new Dictionary<string, object>();
        var header = new MessageHeader();
        header.Populate(properties.Headers);

        var mockConsumer = new Mock<IConsumer<IMessage>>(MockBehavior.Strict);
        mockConsumer.SetupProperty(x => x.QoSPrefetchCount);
        var fatalCalled = false;
        mockConsumer.Object.OnFatal += (sender, args) => fatalCalled = true;

        var broker = new RabbitMQBroker(globalOptions.RabbitOptions!, "RabbitMQBrokerTests");
        broker.StartConsumer(consumerOptions, mockConsumer.Object);

        // Act

        model.BasicPublish("TEST.TestExchange", "", mandatory: true, properties, Encoding.UTF8.GetBytes("Hello"));
        model.WaitForConfirms();
        TestTimelineAwaiter.Await(() => model.MessageCount(consumerOptions.QueueName) == 0);

        // Assert

        Assert.That(fatalCalled, Is.False);
    }

    [Test]
    public void HandleControlMessage_HappyPath_IsOk()
    {
        // Arrange

        var globalOptions = GlobalOptionsForTest();

        var mockControlConsumer = new Mock<IControlMessageConsumer>(MockBehavior.Strict);
        var consumerOptions = new ConsumerOptions
        {
            QueueName = $"Control.{TestContext.CurrentContext.Test.Name}",
            AutoAck = true,
        };
        mockControlConsumer.Setup(x => x.ControlConsumerOptions).Returns(consumerOptions);
        var called = false;
        mockControlConsumer.Setup(x => x.ProcessMessage("hello", "")).Callback(() => called = true);

        using var tester = new MicroserviceTester(globalOptions.RabbitOptions!);
        using var model = tester.Broker.GetModel(TestContext.CurrentContext.Test.Name);
        model.ConfirmSelect();
        model.QueueDeclare(consumerOptions.QueueName, false, true, true, null);
        model.QueueBind(consumerOptions.QueueName, globalOptions.RabbitOptions!.RabbitMqControlExchangeName, "", null);

        var properties = model.CreateBasicProperties();
        var body = Encoding.UTF8.GetBytes("hello");

        var broker = new RabbitMQBroker(globalOptions.RabbitOptions!, "RabbitMQBrokerTests");
        broker.StartControlConsumer(mockControlConsumer.Object);

        // Act

        model.BasicPublish(globalOptions.RabbitOptions!.RabbitMqControlExchangeName!, "", mandatory: true, properties, body);
        model.WaitForConfirms();

        // Assert

        TestTimelineAwaiter.Await(() => called == true);
    }

    [Test]
    public void HandleControlMessage_HappyPathWithValidContentEncoding_IsOk()
    {
        // Arrange

        var globalOptions = GlobalOptionsForTest();

        var mockControlConsumer = new Mock<IControlMessageConsumer>(MockBehavior.Strict);
        var consumerOptions = new ConsumerOptions
        {
            QueueName = $"Control.{TestContext.CurrentContext.Test.Name}",
            AutoAck = true,
        };
        mockControlConsumer.Setup(x => x.ControlConsumerOptions).Returns(consumerOptions);
        var called = false;
        mockControlConsumer.Setup(x => x.ProcessMessage("hello", "")).Callback(() => called = true);

        using var tester = new MicroserviceTester(globalOptions.RabbitOptions!);
        using var model = tester.Broker.GetModel(TestContext.CurrentContext.Test.Name);
        model.ConfirmSelect();
        model.QueueDeclare(consumerOptions.QueueName, false, true, true, null);
        model.QueueBind(consumerOptions.QueueName, globalOptions.RabbitOptions!.RabbitMqControlExchangeName, "", null);

        var properties = model.CreateBasicProperties();
        properties.ContentEncoding = "UTF-8";
        var body = Encoding.UTF8.GetBytes("hello");

        var broker = new RabbitMQBroker(globalOptions.RabbitOptions!, "RabbitMQBrokerTests");
        broker.StartControlConsumer(mockControlConsumer.Object);

        // Act

        model.BasicPublish(globalOptions.RabbitOptions!.RabbitMqControlExchangeName!, "", mandatory: true, properties, body);
        model.WaitForConfirms();

        // Assert

        TestTimelineAwaiter.Await(() => called == true);
    }

    [Test]
    public void HandleControlMessage_HappyPathWithInalidContentEncoding_IsIgnored()
    {
        // Arrange

        var globalOptions = GlobalOptionsForTest();

        var mockControlConsumer = new Mock<IControlMessageConsumer>(MockBehavior.Strict);
        var consumerOptions = new ConsumerOptions
        {
            QueueName = $"Control.{TestContext.CurrentContext.Test.Name}",
            AutoAck = true,
        };
        mockControlConsumer.Setup(x => x.ControlConsumerOptions).Returns(consumerOptions);
        var called = false;
        mockControlConsumer.Setup(x => x.ProcessMessage("hello", "")).Callback(() => called = true);

        using var tester = new MicroserviceTester(globalOptions.RabbitOptions!);
        using var model = tester.Broker.GetModel(TestContext.CurrentContext.Test.Name);
        model.ConfirmSelect();
        model.QueueDeclare(consumerOptions.QueueName, false, true, true, null);
        model.QueueBind(consumerOptions.QueueName, globalOptions.RabbitOptions!.RabbitMqControlExchangeName, "", null);

        var properties = model.CreateBasicProperties();
        properties.ContentEncoding = "invalid";
        var body = Encoding.UTF8.GetBytes("hello");

        var broker = new RabbitMQBroker(globalOptions.RabbitOptions!, "RabbitMQBrokerTests");
        broker.StartControlConsumer(mockControlConsumer.Object);

        // Act

        model.BasicPublish(globalOptions.RabbitOptions!.RabbitMqControlExchangeName!, "", mandatory: true, properties, body);
        model.WaitForConfirms();

        // Assert

        TestTimelineAwaiter.Await(() => called == true);
    }

    [Test]
    public void HandleControlMessage_EmptyBody_IsDiscarded()
    {
        // Arrange

        var globalOptions = GlobalOptionsForTest();

        var mockControlConsumer = new Mock<IControlMessageConsumer>(MockBehavior.Strict);
        var consumerOptions = new ConsumerOptions
        {
            QueueName = "Control.Test123"
        };
        mockControlConsumer.Setup(x => x.ControlConsumerOptions).Returns(consumerOptions);

        using var tester = new MicroserviceTester(globalOptions.RabbitOptions!);
        using var model = tester.Broker.GetModel(TestContext.CurrentContext.Test.Name);
        model.ConfirmSelect();
        model.QueueDeclare(consumerOptions.QueueName, false, true, true, null);
        model.QueueBind(consumerOptions.QueueName, globalOptions.RabbitOptions!.RabbitMqControlExchangeName, "", null);
        var properties = model.CreateBasicProperties();

        var broker = new RabbitMQBroker(globalOptions.RabbitOptions!, "RabbitMQBrokerTests");
        broker.StartControlConsumer(mockControlConsumer.Object);

        // Act

        model.BasicPublish(globalOptions.RabbitOptions!.RabbitMqControlExchangeName!, "", mandatory: true, properties, Array.Empty<byte>());
        model.WaitForConfirms();

        // Assert

        TestTimelineAwaiter.Await(() => model.MessageCount(mockControlConsumer.Object.ControlConsumerOptions.QueueName) == 0);
    }

    [Test]
    public void StartControlConsumer_HappyPath_IsOk()
    {
        // Arrange

        var globalOptions = GlobalOptionsForTest();

        var controlConsumer = new ControlMessageConsumer(globalOptions.RabbitOptions!, TestContext.CurrentContext.Test.Name, 123, globalOptions.RabbitOptions!.RabbitMqControlExchangeName!, (_) => { });
        var broker = new RabbitMQBroker(globalOptions.RabbitOptions!, "RabbitMQBrokerTests");

        // Act

        void call() => broker.StartControlConsumer(controlConsumer);

        // Assert

        Assert.DoesNotThrow(call);
    }

    [Test]
    public void SetupProducer_AfterShutdown_Throws()
    {
        // Arrange

        var globalOptions = GlobalOptionsForTest();
        var broker = new RabbitMQBroker(globalOptions.RabbitOptions!, "RabbitMQBrokerTests");

        // Act

        broker.Shutdown(RabbitMQBroker.DefaultOperationTimeout);

        // Assert

        Assert.That(broker.ShutdownCalled, Is.EqualTo(true));
        var exc = Assert.Throws<ApplicationException>(() => broker.SetupProducer(new ProducerOptions()));
        Assert.That(exc.Message, Is.EqualTo("Adapter has been shut down"));
    }

    [Test]
    public void StopConsumer_CalledTwice_Throws()
    {
        // Arrange

        var consumerOptions = TestConsumerOptions();
        var globalOptions = GlobalOptionsForTest();
        using var tester = new MicroserviceTester(globalOptions.RabbitOptions!, consumerOptions);
        var mockConsumer = new Mock<IConsumer<IMessage>>(MockBehavior.Strict);
        mockConsumer.SetupProperty(x => x.QoSPrefetchCount);
        var consumerId = tester.Broker.StartConsumer(consumerOptions, mockConsumer.Object);

        // Act

        tester.Broker.StopConsumer(consumerId, RabbitMQBroker.DefaultOperationTimeout);

        // Assert

        var exc = Assert.Throws<ApplicationException>(() => tester.Broker.StopConsumer(consumerId, RabbitMQBroker.DefaultOperationTimeout));
        Assert.That(exc.Message, Is.EqualTo("Guid was not found in the task register"));
    }

    [Test]
    public void WaitForConfirms_Repeated_IsOk()
    {
        // Arrange

        var globalOptions = GlobalOptionsForTest();
        using var tester = new MicroserviceTester(globalOptions.RabbitOptions!, TestConsumerOptions());

        var producerOptions = new ProducerOptions
        {
            ExchangeName = "TEST.TestExchange"
        };
        var producerModel = tester.Broker.SetupProducer(producerOptions, true);

        producerModel.SendMessage(new TestMessage(), isInResponseTo: null, routingKey: null);

        // Act

        void call()
        {
            producerModel.WaitForConfirms();
            producerModel.WaitForConfirms();
        }

        // Assert

        Assert.DoesNotThrow(call);
    }

    [Test]
    public void SetupProducer_NullBackoffProvider_DoesNotThrow()
    {
        // Arrange

        var globalOptions = GlobalOptionsForTest();
        using var tester = new MicroserviceTester(globalOptions.RabbitOptions!, TestConsumerOptions());
        var exchangeName = $"TEST.{nameof(SetupProducer_NullBackoffProvider_DoesNotThrow)}Exchange";
        tester.CreateExchange(exchangeName);

        var producerOptions = new ProducerOptions
        {
            ExchangeName = exchangeName,
            BackoffProviderType = null,
        };

        var broker = new RabbitMQBroker(globalOptions.RabbitOptions!, "RabbitMqAdapterTests");

        // Act

        void call() => broker.SetupProducer(producerOptions);

        // Assert

        Assert.DoesNotThrow(call);
    }

    [Test]
    public void SetupProducer_InvalidBackoffProvider_Throws()
    {
        // Arrange

        var globalOptions = GlobalOptionsForTest();
        using var tester = new MicroserviceTester(globalOptions.RabbitOptions!, TestConsumerOptions());
        var exchangeName = $"TEST.{nameof(SetupProducer_InvalidBackoffProvider_Throws)}Exchange";
        tester.CreateExchange(exchangeName);

        var producerOptions = new ProducerOptions
        {
            ExchangeName = exchangeName,
            BackoffProviderType = "Foo",
        };

        var broker = new RabbitMQBroker(globalOptions.RabbitOptions!, "RabbitMqAdapterTests");

        // Act

        void call() => broker.SetupProducer(producerOptions);

        // Assert

        var exc = Assert.Throws<ArgumentException>(call);
        Assert.That(exc.Message, Is.EqualTo("Could not parse 'Foo' to a valid BackoffProviderType"));
    }

    [Test]
    public void SetupProducer_ValidBackoffProvider_IsOk()
    {
        // Arrange

        var globalOptions = GlobalOptionsForTest();
        using var tester = new MicroserviceTester(globalOptions.RabbitOptions!, TestConsumerOptions());
        var exchangeName = $"TEST.{nameof(SetupProducer_InvalidBackoffProvider_Throws)}Exchange";
        tester.CreateExchange(exchangeName);

        var producerOptions = new ProducerOptions
        {
            ExchangeName = exchangeName,
            BackoffProviderType = "StaticBackoffProvider",
        };

        var broker = new RabbitMQBroker(globalOptions.RabbitOptions!, "RabbitMqAdapterTests");

        // Act

        void call() => broker.SetupProducer(producerOptions);

        // Assert

        Assert.DoesNotThrow(call);
    }

    [Test]
    public void Constructor_InvalidHostName_Throws()
    {
        // Arrange

        var globalOptions = GlobalOptionsForTest();

        // Act

        void call() => _ = new RabbitMQBroker(globalOptions.RabbitOptions!, "   ");

        // Assert

        var exc = Assert.Throws<ArgumentException>(call);
        Assert.That(exc.Message, Is.EqualTo("RabbitMQ host ID required (Parameter 'hostId')"));
    }

    private class TestMessage : IMessage { }
}
