
using RabbitMQ.Client;
using Smi.Common;
using Smi.Common.Execution;
using Smi.Common.Messages;
using Smi.Common.Messaging;
using Smi.Common.Options;
using System;
using System.Collections.Generic;

namespace Smi.Common.Tests
{
    public class MicroserviceTester : IDisposable
    {
        private readonly RabbitMqAdapter _adapter;

        private readonly Dictionary<ConsumerOptions, IProducerModel> _sendToConsumers = new Dictionary<ConsumerOptions, IProducerModel>();

        private readonly List<string> _declaredExchanges = new List<string>();
        private readonly List<string> _declaredQueues = new List<string>();
        private readonly ConnectionFactory _factory;

        /// <summary>
        /// When true, will delete any created queues/exchanges when Dispose is called. Can set to false to inspect
        /// queue messages before they are deleted.
        /// 
        /// <para>Defaults to true</para>
        /// </summary>
        public bool CleanUpAfterTest { get; set; }


        /// <summary>
        /// Hosts to call Stop on in the Dispose step.  This ensures that all hosts are correctly shutdown even if Exceptions
        /// are thrown in test (provided the MicroserviceTester is in a using statement).
        /// </summary>
        public HashSet<MicroserviceHost> StopOnDispose = new HashSet<MicroserviceHost>();

        public MicroserviceTester(RabbitOptions rabbitOptions, params ConsumerOptions[] peopleYouWantToSendMessagesTo)
        {
            CleanUpAfterTest = true;

            _adapter = new RabbitMqAdapter(rabbitOptions.CreateConnectionFactory(), "TestHost");

            _factory = new ConnectionFactory
            {
                HostName = rabbitOptions.RabbitMqHostName,
                Port = rabbitOptions.RabbitMqHostPort,
                VirtualHost = rabbitOptions.RabbitMqVirtualHost,
                UserName = rabbitOptions.RabbitMqUserName,
                Password = rabbitOptions.RabbitMqPassword
            };

            using (var con = _factory.CreateConnection())
            using (var model = con.CreateModel())
            {
                //get rid of old exchanges
                model.ExchangeDelete(rabbitOptions.RabbitMqControlExchangeName);
                //create a new one
                model.ExchangeDeclare(rabbitOptions.RabbitMqControlExchangeName, ExchangeType.Topic, true);

                //setup a sender chanel for each of the consumers you want to test sending messages to
                foreach (ConsumerOptions consumer in peopleYouWantToSendMessagesTo)
                {
                    if (!consumer.QueueName.Contains("TEST."))
                        consumer.QueueName = consumer.QueueName.Insert(0, "TEST.");

                    var exchangeName = consumer.QueueName.Replace("Queue", "Exchange");

                    //terminate any old queues / exchanges
                    model.ExchangeDelete(exchangeName);
                    model.QueueDelete(consumer.QueueName);
                    _declaredExchanges.Add(exchangeName);

                    //Create a binding between the exchange and the queue
                    model.ExchangeDeclare(exchangeName, ExchangeType.Direct, true);//durable seems to be needed because RabbitMQAdapter wants it?
                    model.QueueDeclare(consumer.QueueName, true, false, false);//shared with other users
                    model.QueueBind(consumer.QueueName, exchangeName, "");
                    _declaredQueues.Add(consumer.QueueName);

                    //Create a producer which can send to the 
                    var producerOptions = new ProducerOptions
                    {
                        ExchangeName = exchangeName
                    };

                    _sendToConsumers.Add(consumer, _adapter.SetupProducer(producerOptions, true));
                }
            }
        }

        /// <summary>
        /// Sends the given message to your consumer, you must have passed the consumer into the MicroserviceTester constructor since all adapter setup happens via option
        /// at RabbitMQAdapter construction time
        /// </summary>
        /// <param name="toConsumer"></param>
        /// <param name="msg"></param>
        public void SendMessage(ConsumerOptions toConsumer, IMessage msg)
        {
            _sendToConsumers[toConsumer].SendMessage(msg, null);
            _sendToConsumers[toConsumer].WaitForConfirms();
        }

        /// <summary>
        /// Sends the given message to your consumer, you must have passed the consumer into the MicroserviceTester constructor since all adapter setup happens via option
        /// at RabbitMQAdapter construction time
        /// </summary>
        /// <param name="toConsumer"></param>
        /// <param name="messages"></param>
        /// <param name="b"></param>
        public void SendMessages(ConsumerOptions toConsumer, IEnumerable<IMessage> messages, bool generateIMessageHeaders)
        {
            foreach (IMessage msg in messages)
                _sendToConsumers[toConsumer].SendMessage(msg, generateIMessageHeaders ? new MessageHeader() : null);

            _sendToConsumers[toConsumer].WaitForConfirms();
        }

        /// <summary>
        /// Sends the given message to your consumer, you must have passed the consumer into the MicroserviceTester constructor since all adapter setup happens via option
        /// at RabbitMQAdapter construction time
        /// </summary>
        /// <param name="toConsumer"></param>
        /// <param name="header"></param>
        /// <param name="msg"></param>
        public void SendMessage(ConsumerOptions toConsumer, IMessageHeader header, IMessage msg)
        {
            _sendToConsumers[toConsumer].SendMessage(msg, header);
            _sendToConsumers[toConsumer].WaitForConfirms();
        }

        /// <summary>
        /// Creates a self titled RabbitMQ exchange/queue pair where the name of the exchange is the ProducerOptions.ExchangeName and the queue has the same name.
        /// This will delete and recreate the exchange if it already exists (ensuring no old messages are stuck floating around). 
        /// </summary>
        /// <param name="producer"></param>
        /// <param name="consumerIfAny"></param>
        /// <param name="isSecondaryBinding">false to create an entirely new Exchange=>Queue (including deleting any existing queue/exchange). False to simply declare the 
        /// queue and bind it to the exchange which is assumed to already exist (this allows you to set up exchange=>multiple queues).  If you are setting up multiple queues
        /// from a single exchange the first call should be isSecondaryBinding = false and all further calls after that for the same exchange should be isSecondaryBinding=true </param>
        public void CreateExchange(string exchangeName, ConsumerOptions consumerIfAny, bool isSecondaryBinding = false)
        {
            if (!exchangeName.Contains("TEST."))
                exchangeName = exchangeName.Insert(0, "TEST.");

            string queueName = consumerIfAny != null ? consumerIfAny.QueueName : exchangeName;

            using (var con = _factory.CreateConnection())
            using (var model = con.CreateModel())
            {
                //setup a sender channel for each of the consumers you want to test sending messages to

                //terminate any old queues / exchanges
                if (!isSecondaryBinding)
                    model.ExchangeDelete(exchangeName);

                model.QueueDelete(queueName);

                //Create a binding between the exchange and the queue
                if (!isSecondaryBinding)
                    model.ExchangeDeclare(exchangeName, ExchangeType.Direct, true);//durable seems to be needed because RabbitMQAdapter wants it?

                model.QueueDeclare(queueName, true, false, false); //shared with other users
                model.QueueBind(queueName, exchangeName, "");

                Console.WriteLine("Created Exchange " + exchangeName + "=>" + queueName);
            }
        }

        /// <summary>
        /// Shuts down the tester without performing cleanup of any declared queues / exchanges
        /// </summary>
        public void Shutdown()
        {
            foreach (MicroserviceHost host in StopOnDispose)
                host.Stop("MicroserviceTester Disposed");

            _adapter.Shutdown(RabbitMqAdapter.DefaultOperationTimeout);
        }

        /// <summary>
        /// Deletes any declared queues / exchanges depending on the CleanUpAfterTest option, then calls shutdown
        /// </summary>
        public void Dispose()
        {
            Shutdown();

            if (CleanUpAfterTest)
            {
                using (IConnection conn = _factory.CreateConnection())
                using (IModel model = conn.CreateModel())
                {
                    _declaredExchanges.ForEach(x => model.ExchangeDelete(x));
                    _declaredQueues.ForEach(x => model.QueueDelete(x));
                }
            }
        }
    }
}
