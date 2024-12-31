using Moq;
using NUnit.Framework;
using NUnit.Framework.Internal;
using SmiServices.Common.Messages;
using SmiServices.Common.Messaging;
using SmiServices.Common.Options;
using SmiServices.UnitTests.Common;
using System;

namespace SmiServices.IntegrationTests.Common.Messaging
{
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

        private static GlobalOptions GlobalOptionsFor(string testName)
            => new GlobalOptionsFactory().Load(testName);

        [TestCase(null)]
        [TestCase("  ")]
        public void SetupProducer_InvalidExchangeName_Throws(string? exchangeName)
        {
            // Arrange

            var producerOptions = new ProducerOptions
            {
                ExchangeName = exchangeName
            };
            var globalOptions = GlobalOptionsFor(nameof(SetupProducer_InvalidExchangeName_Throws));
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

            var globalOptions = GlobalOptionsFor(nameof(SetupProducer_MissingExchange_Throws));
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

            var globalOptions = GlobalOptionsFor(nameof(StartConsumer_MissingQueue_Throws));
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

            var globalOptions = GlobalOptionsFor(nameof(StartConsumer_MissingQueue_Throws));
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

            var globalOptions = GlobalOptionsFor(nameof(Shutdown_NoTimeout_Throws));
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
        public void SetupConsumer_AfterShutdown_Throws()
        {
            // Arrange

            var globalOptions = GlobalOptionsFor(nameof(SetupConsumer_AfterShutdown_Throws));
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
        public void SetupProducer_AfterShutdown_Throws()
        {
            // Arrange

            var globalOptions = GlobalOptionsFor(nameof(SetupProducer_AfterShutdown_Throws));
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
            var globalOptions = GlobalOptionsFor(nameof(StopConsumer_CalledTwice_Throws));
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

            var globalOptions = GlobalOptionsFor(nameof(WaitForConfirms_Repeated_IsOk));
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

            var globalOptions = GlobalOptionsFor(nameof(SetupProducer_NullBackoffProvider_DoesNotThrow));
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

            var globalOptions = GlobalOptionsFor(nameof(SetupProducer_InvalidBackoffProvider_Throws));
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

            var globalOptions = GlobalOptionsFor(nameof(SetupProducer_ValidBackoffProvider_IsOk));
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

        private class TestMessage : IMessage { }
    }
}
