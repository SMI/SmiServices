using Moq;
using NLog;
using NLog.Targets;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using SmiServices.Common.Messages;
using SmiServices.Common.Messaging;
using SmiServices.Common.Options;
using SmiServices.UnitTests.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;

namespace SmiServices.IntegrationTests.Common.Messaging
{
    [TestFixture, RequiresRabbit]
    public class RabbitMQBrokerTests
    {
        private ProducerOptions _testProducerOptions = null!;
        private ConsumerOptions _testConsumerOptions = null!;

        private MicroserviceTester _tester = null!;

        private Consumer<IMessage> _mockConsumer = null!;
        private GlobalOptions _testOptions = null!;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestLogger.Setup();
            _testOptions = new GlobalOptionsFactory().Load(nameof(RabbitMQBrokerTests));

            _testProducerOptions = new ProducerOptions
            {
                ExchangeName = "TEST.TestExchange"
            };

            _testConsumerOptions = new ConsumerOptions
            {
                QueueName = "TEST.TestQueue",
                QoSPrefetchCount = 1,
                AutoAck = false
            };

            _mockConsumer = Mock.Of<Consumer<IMessage>>();
            _tester = new MicroserviceTester(_testOptions.RabbitOptions!, _testConsumerOptions);
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            _tester.Shutdown();
            _tester.Dispose();
        }

        /// <summary>
        /// Tests that we get an error if the exchange name provided is invalid or does not exist on the server
        /// </summary>
        [Test]
        public void TestSetupProducerThrowsOnNonExistentExchange()
        {
            var producerOptions = new ProducerOptions
            {
                ExchangeName = null
            };

            Assert.Throws<ArgumentException>(() => _tester.Broker.SetupProducer(producerOptions));

            producerOptions.ExchangeName = "TEST.DoesNotExistExchange";
            Assert.Throws<ApplicationException>(() => _tester.Broker.SetupProducer(producerOptions));
        }

        /// <summary>
        /// Checks that the queue exists and we get an exception if not
        /// </summary>
        [Test]
        public void TestStartConsumerThrowsOnNonExistentQueue()
        {
            var oldq = _testConsumerOptions.QueueName;
            _testConsumerOptions.QueueName = $"TEST.WrongQueue{new Random().NextInt64()}";
            Assert.Throws<ApplicationException>(() => _tester.Broker.StartConsumer(_testConsumerOptions, _mockConsumer));
            _testConsumerOptions.QueueName = oldq;
        }

        /// <summary>
        /// This shouldn't hang the test TaskRunner process if everything goes well
        /// </summary>
        [Test]
        public void TestShutdownExitsProperly()
        {
            // Setup some consumers/producers so some channels are created
            _tester.Broker.SetupProducer(_testProducerOptions);
            _tester.Broker.StartConsumer(_testConsumerOptions, _mockConsumer);
        }

        /// <summary>
        /// Tests that we throw an exception if a task does not exit quickly after cancellation has been requested
        /// </summary>
        [Test]
        public void TestShutdownThrowsOnTimeout()
        {
            var testAdapter = new RabbitMQBroker(_testOptions.RabbitOptions!, "RabbitMQBrokerTests");
            testAdapter.StartConsumer(_testConsumerOptions, _mockConsumer);
            Assert.Throws<ApplicationException>(() => testAdapter.Shutdown(TimeSpan.Zero));
        }

        /// <summary>
        /// Tests that we can't open any new connections after we have called shutdown
        /// </summary>
        [Test]
        public void TestNoNewConnectionsAfterShutdown()
        {
            var testAdapter = new RabbitMQBroker(_testOptions.RabbitOptions!, "RabbitMQBrokerTests");
            Assert.That(testAdapter.ShutdownCalled, Is.False);

            testAdapter.Shutdown(RabbitMQBroker.DefaultOperationTimeout);

            Assert.That(testAdapter.ShutdownCalled, Is.True);
            Assert.Throws<ApplicationException>(() => testAdapter.StartConsumer(_testConsumerOptions, _mockConsumer));
            Assert.Throws<ApplicationException>(() => testAdapter.SetupProducer(_testProducerOptions));
        }

        [Test]
        public void TestStopConsumer()
        {
            var consumerId = _tester.Broker.StartConsumer(_testConsumerOptions, _mockConsumer);
            Assert.DoesNotThrow(() => _tester.Broker.StopConsumer(consumerId, RabbitMQBroker.DefaultOperationTimeout));
            Assert.Throws<ApplicationException>(() => _tester.Broker.StopConsumer(consumerId, RabbitMQBroker.DefaultOperationTimeout));
        }

        [Test]
        public void TestGetRabbitServerVersion()
        {
            var testFactory = new ConnectionFactory
            {
                HostName = _testOptions.RabbitOptions!.RabbitMqHostName,
                VirtualHost = _testOptions.RabbitOptions.RabbitMqVirtualHost,
                Port = _testOptions.RabbitOptions.RabbitMqHostPort,
                UserName = _testOptions.RabbitOptions.RabbitMqUserName,
                Password = _testOptions.RabbitOptions.RabbitMqPassword
            };

            using var connection = testFactory.CreateConnection();
            // These are all the server properties we can check using the connection
            PrintObjectDictionary(connection.ServerProperties);

            Assert.That(connection.ServerProperties.ContainsKey("version"), Is.True);
        }

        [Test]
        public void TestMultipleConfirmsOk()
        {
            var pm = _tester.Broker.SetupProducer(_testProducerOptions, true);

            pm.SendMessage(new TestMessage(), isInResponseTo: null, routingKey: null);

            for (var i = 0; i < 10; ++i)
                pm.WaitForConfirms();
        }

        [Test]
        public void TestMultipleCloseOk()
        {
            var fact = new ConnectionFactory
            {
                HostName = _testOptions.RabbitOptions!.RabbitMqHostName,
                VirtualHost = _testOptions.RabbitOptions.RabbitMqVirtualHost,
                Port = _testOptions.RabbitOptions.RabbitMqHostPort,
                UserName = _testOptions.RabbitOptions.RabbitMqUserName,
                Password = _testOptions.RabbitOptions.RabbitMqPassword
            };

            var conn = fact.CreateConnection("TestConn");
            var model = conn.CreateModel();

            // Closing model twice is ok
            model.Close(200, "bye");
            model.Close(200, "bye");

            // Closing connection twice is NOT ok
            conn.Close(200, "bye");
            Assert.Throws<ChannelClosedException>(() => conn.Close(200, "bye"));

            // Closing model after connection is ok
            model.Close(200, "bye bye");

            Assert.Multiple(() =>
            {
                Assert.That(model.IsOpen, Is.False);
                Assert.That(conn.IsOpen, Is.False);
            });
        }

        [Test]
        public void TestWaitAfterChannelClosed()
        {
            var testAdapter = new RabbitMQBroker(_testOptions.RabbitOptions!, "RabbitMqAdapterTests");
            var model = testAdapter.GetModel("TestConnection");
            model.ConfirmSelect();

            testAdapter.Shutdown(RabbitMQBroker.DefaultOperationTimeout);

            Assert.That(model.IsClosed, Is.True);
            Assert.Throws<AlreadyClosedException>(() => model.WaitForConfirms());
        }

        [TestCase(typeof(SelfClosingConsumer))]
        [TestCase(typeof(DoNothingConsumer))]
        public void Test_Shutdown(Type consumerType)
        {
            MemoryTarget target = new()
            {
                Layout = "${message}"
            };

            LogManager.Setup().LoadConfiguration(x => x.ForLogger(LogLevel.Debug).WriteTo(target));

            var o = new GlobalOptionsFactory().Load(nameof(Test_Shutdown));

            var consumer = (IConsumer?)Activator.CreateInstance(consumerType);

            //connect to rabbit with a new consumer
            using var tester = new MicroserviceTester(o.RabbitOptions!, [_testConsumerOptions]) { CleanUpAfterTest = false };
            tester.Broker.StartConsumer(_testConsumerOptions, consumer!, true);

            //send a message to trigger consumer behaviour
            tester.SendMessage(_testConsumerOptions, new TestMessage());

            //give the message time to get picked up
            Thread.Sleep(3000);

            //now attempt to shut down adapter
            tester.Broker.Shutdown(RabbitMQBroker.DefaultOperationTimeout);

            var expectedErrorMessage = consumer switch
            {
                SelfClosingConsumer => "exiting (channel is closed)",
                DoNothingConsumer => "exiting (shutdown was called)",
                _ => "nothing to see here"
            };

            Assert.That(target.Logs.Any(s => s.Contains(expectedErrorMessage)), Is.True, $"Expected message {expectedErrorMessage} was not found, messages were:" + string.Join(Environment.NewLine, target.Logs));
        }

        [Test]
        public void MessageHolds()
        {
            var consumerOptions = new ConsumerOptions
            {
                QueueName = "TEST.TestQueue",
                QoSPrefetchCount = 1,
                AutoAck = false,
                HoldUnprocessableMessages = true,
            };
            var consumer = new ThrowingConsumer();

            using var tester = new MicroserviceTester(_testOptions.RabbitOptions!, [consumerOptions]);
            tester.Broker.StartConsumer(consumerOptions, consumer!, true);

            tester.SendMessage(consumerOptions, new TestMessage());
            Thread.Sleep(500);

            Assert.Multiple(() =>
            {
                Assert.That(consumer.HeldMessages, Is.EqualTo(1));
                Assert.That(consumer.AckCount, Is.EqualTo(0));
            });
        }

        [Test]
        public void SetupProducer_NullBackoffProvider_DoesNotThrow()
        {
            // Arrange

            var exchangeName = $"TEST.{nameof(SetupProducer_NullBackoffProvider_DoesNotThrow)}Exchange";
            _tester.CreateExchange(exchangeName);

            var producerOptions = new ProducerOptions
            {
                ExchangeName = exchangeName,
                BackoffProviderType = null,
            };

            var broker = new RabbitMQBroker(_testOptions.RabbitOptions!, "RabbitMqAdapterTests");

            // Act
            // Assert
            Assert.DoesNotThrow(() => broker.SetupProducer(producerOptions));
        }

        [Test]
        public void SetupProducer_InvalidBackoffProvider_Throws()
        {
            // Arrange

            var exchangeName = $"TEST.{nameof(SetupProducer_InvalidBackoffProvider_Throws)}Exchange";
            _tester.CreateExchange(exchangeName);

            var producerOptions = new ProducerOptions
            {
                ExchangeName = exchangeName,
                BackoffProviderType = "Foo",
            };

            var broker = new RabbitMQBroker(_testOptions.RabbitOptions!, "RabbitMqAdapterTests");

            // Act
            // Assert
            var exc = Assert.Throws<ArgumentException>(() => broker.SetupProducer(producerOptions));
            Assert.That(exc.Message, Is.EqualTo("Could not parse 'Foo' to a valid BackoffProviderType"));
        }

        [Test]
        public void SetupProducer_ValidBackoffProvider()
        {
            // Arrange

            var exchangeName = $"TEST.{nameof(SetupProducer_InvalidBackoffProvider_Throws)}Exchange";
            _tester.CreateExchange(exchangeName);

            var producerOptions = new ProducerOptions
            {
                ExchangeName = exchangeName,
                BackoffProviderType = "StaticBackoffProvider",
            };

            var broker = new RabbitMQBroker(_testOptions.RabbitOptions!, "RabbitMqAdapterTests");

            // Act
            // Assert
            Assert.DoesNotThrow(() => broker.SetupProducer(producerOptions));
        }

        private class ThrowingConsumer : Consumer<TestMessage>
        {
            public int HeldMessages { get => _heldMessages; }

            protected override void ProcessMessageImpl(IMessageHeader header, TestMessage msg, ulong tag)
            {
                throw new Exception("Throwing!");
            }
        }

        private class TestMessage : IMessage { }


        private static void PrintObjectDictionary(IDictionary<string, object> dictionary)
        {
            foreach (var prop in dictionary)
            {
                if (prop.Value is byte[] bytes)
                {
                    Console.WriteLine($"{prop.Key}:\t{Encoding.UTF8.GetString(bytes)}");
                }
                else if (prop.Value is IDictionary<string, object> value)
                {
                    Console.WriteLine($"{prop.Key}:");
                    PrintObjectDictionary(value);
                }
                else
                {
                    Console.WriteLine($"{prop.Key}:\t{prop.Value}");
                }
            }
        }
    }
}
