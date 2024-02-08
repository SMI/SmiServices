
using Moq;
using NLog;
using NLog.Targets;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Smi.Common.Messages;
using Smi.Common.Messaging;
using Smi.Common.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;

namespace Smi.Common.Tests.Messaging
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
            Assert.False(testAdapter.ShutdownCalled);

            testAdapter.Shutdown(RabbitMQBroker.DefaultOperationTimeout);

            Assert.True(testAdapter.ShutdownCalled);
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

            Assert.True(connection.ServerProperties.ContainsKey("version"));
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

            Assert.False(model.IsOpen);
            Assert.False(conn.IsOpen);
        }

        [Test]
        public void TestWaitAfterChannelClosed()
        {
            var testAdapter = new RabbitMQBroker(_testOptions.RabbitOptions!, "RabbitMqAdapterTests");
            var model = testAdapter.GetModel("TestConnection");
            model.ConfirmSelect();

            testAdapter.Shutdown(RabbitMQBroker.DefaultOperationTimeout);

            Assert.True(model.IsClosed);
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
            using var tester = new MicroserviceTester(o.RabbitOptions!, new[] { _testConsumerOptions }) { CleanUpAfterTest = false };
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

            Assert.IsTrue(target.Logs.Any(s => s.Contains(expectedErrorMessage)), $"Expected message {expectedErrorMessage} was not found, messages were:" + string.Join(Environment.NewLine, target.Logs));
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

            using var tester = new MicroserviceTester(_testOptions.RabbitOptions!, new[] { consumerOptions });
            tester.Broker.StartConsumer(consumerOptions, consumer!, true);

            tester.SendMessage(consumerOptions, new TestMessage());
            Thread.Sleep(500);

            Assert.AreEqual(1, consumer.HeldMessages);
            Assert.AreEqual(0, consumer.AckCount);
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
