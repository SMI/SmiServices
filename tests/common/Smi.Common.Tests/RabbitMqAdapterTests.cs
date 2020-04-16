
using Moq;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Smi.Common.Messages;
using Smi.Common.Messaging;
using Smi.Common.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using NLog;
using NLog.Targets;

namespace Smi.Common.Tests
{
    [TestFixture, RequiresRabbit]
    public class RabbitMqAdapterTests
    {
        private RabbitMqAdapter _testAdapter;

        private ProducerOptions _testProducerOptions;
        private ConsumerOptions _testConsumerOptions;

        private MicroserviceTester _tester;

        private Consumer _mockConsumer;
        private GlobalOptions _testOptions;


        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestLogger.Setup();
        }

        [SetUp]
        public void SetUp()
        {
            _testOptions = GlobalOptions.Load("default.yaml", TestContext.CurrentContext.TestDirectory);

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

            _mockConsumer = Mock.Of<Consumer>();

            _testAdapter = new RabbitMqAdapter(_testOptions.RabbitOptions.CreateConnectionFactory(), "RabbitMqAdapterTests");

            _tester = new MicroserviceTester(_testOptions.RabbitOptions, _testConsumerOptions);
        }

        [TearDown]
        public void TearDown()
        {
            if (_testAdapter != null)
                _testAdapter.Shutdown(RabbitMqAdapter.DefaultOperationTimeout);

            _tester.Shutdown();
        }

        /// <summary>
        /// Tests that we get an error if the exchange name provided is invalid or does not exist on the server
        /// </summary>
        [Test]
        public void TestSetupProducerThrowsOnNonExistentExchange()
        {
            var producerOptions = new ProducerOptions();

            producerOptions.ExchangeName = null;
            Assert.Throws<ArgumentException>(() => _testAdapter.SetupProducer(producerOptions));

            producerOptions.ExchangeName = "TEST.DoesNotExistExchange";
            Assert.Throws<ApplicationException>(() => _testAdapter.SetupProducer(producerOptions));
        }

        /// <summary>
        /// Checks that the queue exists and we get an exception if not
        /// </summary>
        [Test]
        public void TestStartConsumerThrowsOnNonExistentQueue()
        {
            _testConsumerOptions.QueueName = "TEST.WrongQueue";
            Assert.Throws<ApplicationException>(() => _testAdapter.StartConsumer(_testConsumerOptions, _mockConsumer));
        }

        /// <summary>
        /// This shouldn't hang the test TaskRunner process if everything goes well
        /// </summary>
        [Test]
        public void TestShutdownExitsProperly()
        {
            // Setup some consumers/producers so some channels are created
            _testAdapter.SetupProducer(_testProducerOptions);
            _testAdapter.StartConsumer(_testConsumerOptions, _mockConsumer);
        }

        /// <summary>
        /// Tests that we throw an exception if a task does not exit quickly after cancellation has been requested
        /// </summary>
        [Test]
        public void TestShutdownThrowsOnTimeout()
        {
            _testAdapter.StartConsumer(_testConsumerOptions, _mockConsumer);
            Assert.Throws<ApplicationException>(() => _testAdapter.Shutdown(TimeSpan.Zero));
            _testAdapter = null;
        }

        /// <summary>
        /// Tests that we can't open any new connections after we have called shutdown
        /// </summary>
        [Test]
        public void TestNoNewConnectionsAfterShutdown()
        {
            Assert.False(_testAdapter.ShutdownCalled);

            _testAdapter.Shutdown(RabbitMqAdapter.DefaultOperationTimeout);

            Assert.True(_testAdapter.ShutdownCalled);
            Assert.Throws<ApplicationException>(() => _testAdapter.StartConsumer(_testConsumerOptions, _mockConsumer));
            Assert.Throws<ApplicationException>(() => _testAdapter.SetupProducer(_testProducerOptions));
        }

        [Test]
        public void TestStopConsumer()
        {
            Guid consumerId = _testAdapter.StartConsumer(_testConsumerOptions, _mockConsumer);
            Assert.DoesNotThrow(() => _testAdapter.StopConsumer(consumerId, RabbitMqAdapter.DefaultOperationTimeout));
            Assert.Throws<ApplicationException>(() => _testAdapter.StopConsumer(consumerId, RabbitMqAdapter.DefaultOperationTimeout));
        }

        [Test]
        public void TestGetRabbitServerVersion()
        {
            var testFactory = new ConnectionFactory
            {
                HostName = _testOptions.RabbitOptions.RabbitMqHostName,
                VirtualHost = _testOptions.RabbitOptions.RabbitMqVirtualHost,
                Port = _testOptions.RabbitOptions.RabbitMqHostPort,
                UserName = _testOptions.RabbitOptions.RabbitMqUserName,
                Password = _testOptions.RabbitOptions.RabbitMqPassword
            };

            using (IConnection connection = testFactory.CreateConnection())
            {
                // These are all the server properties we can check using the connection
                PrintObjectDictionary(connection.ServerProperties);

                Assert.True(connection.ServerProperties.ContainsKey("version"));
            }
        }

        [Test]
        public void TestMultipleConfirmsOk()
        {
            IProducerModel pm = _testAdapter.SetupProducer(_testProducerOptions, true);

            pm.SendMessage(new TestMessage(), null);

            for (var i = 0; i < 10; ++i)
                pm.WaitForConfirms();
        }

        [Test]
        public void TestMultipleCloseOk()
        {
            var fact = new ConnectionFactory
            {
                HostName = _testOptions.RabbitOptions.RabbitMqHostName,
                VirtualHost = _testOptions.RabbitOptions.RabbitMqVirtualHost,
                Port = _testOptions.RabbitOptions.RabbitMqHostPort,
                UserName = _testOptions.RabbitOptions.RabbitMqUserName,
                Password = _testOptions.RabbitOptions.RabbitMqPassword
            };

            IConnection conn = fact.CreateConnection("TestConn");
            IModel model = conn.CreateModel();

            // Closing model twice is ok
            model.Close(200, "bye");
            model.Close(200, "bye");

            // Closing connection twice is ok
            conn.Close(200, "bye");
            conn.Close(200, "bye");

            // Closing model after connection is ok
            model.Close(200, "bye bye");

            Assert.False(model.IsOpen);
            Assert.False(conn.IsOpen);
        }

        [Test]
        public void TestWaitAfterChannelClosed()
        {
            IModel model = _testAdapter.GetModel("TestConnection");
            model.ConfirmSelect();

            _testAdapter.Shutdown(RabbitMqAdapter.DefaultOperationTimeout);

            Assert.True(model.IsClosed);
            Assert.Throws<AlreadyClosedException>(() => model.WaitForConfirms());
        }

        [TestCase(typeof(SelfClosingConsumer))]
        [TestCase(typeof(DoNothingConsumer))]
        public void Test_Shutdown(Type consumerType)
        {
            MemoryTarget target = new MemoryTarget();                                                  
            target.Layout = "${message}";                                                              
                                                                                           
            NLog.Config.SimpleConfigurator.ConfigureForTargetLogging(target, LogLevel.Debug);          

            var o = GlobalOptions.Load("default.yaml", TestContext.CurrentContext.TestDirectory);

            var consumer = (IConsumer)Activator.CreateInstance(consumerType);

            //connect to rabbit with a new consumer
            using (var tester = new MicroserviceTester(o.RabbitOptions, new []{_testConsumerOptions}))
            {
                _testAdapter.StartConsumer(_testConsumerOptions, consumer, true);
                
                //send a message to trigger consumer behaviour
                tester.SendMessage(_testConsumerOptions,new TestMessage());

                //give the message time to get picked up
                Thread.Sleep(3000);
                
                //now attempt to shut down adapter
                _testAdapter.Shutdown(RabbitMqAdapter.DefaultOperationTimeout);

                string expectedErrorMessage = "nothing to see here";

                if (consumer is SelfClosingConsumer)
                    expectedErrorMessage = "exiting (channel is closed)";
                if (consumer is DoNothingConsumer)
                    expectedErrorMessage = "exiting (cancellation was requested)";

                Assert.IsTrue(target.Logs.Any(s=>s.Contains(expectedErrorMessage)),"Expected message was not found, messages were:" + string.Join(Environment.NewLine,target.Logs));
            }
        }

        [Test]
        public void TestAckAfterNack()
        {
            Assert.Inconclusive("Not impl");
        }

        private class TestMessage : IMessage { }


        private static void PrintObjectDictionary(IDictionary<string, object> dictionary)
        {
            foreach (KeyValuePair<string, object> prop in dictionary)
            {
                if (prop.Value as byte[] != null)
                {
                    Console.WriteLine(prop.Key + ":\t" + Encoding.UTF8.GetString((byte[])prop.Value));
                }
                else if (prop.Value as IDictionary<string, object> != null)
                {
                    Console.WriteLine(prop.Key + ":");
                    PrintObjectDictionary((IDictionary<string, object>)prop.Value);
                }
                else
                {
                    Console.WriteLine(prop.Key + ":\t" + prop.Value);
                }
            }
        }
    }
}
