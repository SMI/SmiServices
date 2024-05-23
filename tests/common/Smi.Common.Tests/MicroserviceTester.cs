
using RabbitMQ.Client;
using Smi.Common.Execution;
using Smi.Common.Messages;
using Smi.Common.MessageSerialization;
using Smi.Common.Messaging;
using Smi.Common.Options;
using System;
using System.Collections.Generic;
using System.Text;


namespace Smi.Common.Tests
{
    public class MicroserviceTester : IDisposable
    {
        public readonly RabbitMQBroker Broker;

        private readonly Dictionary<ConsumerOptions, IProducerModel> _sendToConsumers = new();

        private readonly List<string> _declaredExchanges = new();
        private readonly List<string> _declaredQueues = new();

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
        public HashSet<MicroserviceHost> StopOnDispose = new();

        public MicroserviceTester(RabbitOptions rabbitOptions, params ConsumerOptions[] peopleYouWantToSendMessagesTo)
        {
            CleanUpAfterTest = true;

            Broker = new RabbitMQBroker(rabbitOptions, "TestHost");

            using var model = Broker.GetModel(nameof(MicroserviceTester));
            //setup a sender channel for each of the consumers you want to test sending messages to
            foreach (ConsumerOptions consumer in peopleYouWantToSendMessagesTo)
            {
                if (!consumer.QueueName!.Contains("TEST."))
                    consumer.QueueName = consumer.QueueName.Insert(0, "TEST.");

                var exchangeName = consumer.QueueName.Replace("Queue", "Exchange");

                //terminate any old queues / exchanges
                model.ExchangeDelete(exchangeName);
                model.QueueDelete(consumer.QueueName);
                _declaredExchanges.Add(exchangeName);

                //Create a binding between the exchange and the queue
                model.ExchangeDeclare(exchangeName, ExchangeType.Direct, true);//durable seems to be needed because MessageBroker wants it?
                model.QueueDeclare(consumer.QueueName, true, false, false);//shared with other users
                model.QueueBind(consumer.QueueName, exchangeName, "");
                _declaredQueues.Add(consumer.QueueName);

                //Create a producer which can send to the 
                var producerOptions = new ProducerOptions
                {
                    ExchangeName = exchangeName
                };

                _sendToConsumers.Add(consumer, Broker.SetupProducer(producerOptions, true));
            }
        }

        /// <summary>
        /// Sends the given message to your consumer, you must have passed the consumer into the MicroserviceTester constructor since all adapter setup happens via option
        /// at MessageBroker construction time
        /// </summary>
        /// <param name="toConsumer"></param>
        /// <param name="msg"></param>
        public void SendMessage(ConsumerOptions toConsumer, IMessage msg)
        {
            _sendToConsumers[toConsumer].SendMessage(msg, isInResponseTo: null, routingKey: null);
            _sendToConsumers[toConsumer].WaitForConfirms();
        }

        /// <summary>
        /// Sends the given message to your consumer, you must have passed the consumer into the MicroserviceTester constructor since all adapter setup happens via option
        /// at MessageBroker construction time
        /// </summary>
        /// <param name="toConsumer"></param>
        /// <param name="messages"></param>
        /// <param name="generateIMessageHeaders"></param>
        public void SendMessages(ConsumerOptions toConsumer, IEnumerable<IMessage> messages, bool generateIMessageHeaders)
        {
            foreach (IMessage msg in messages)
                _sendToConsumers[toConsumer].SendMessage(msg, generateIMessageHeaders ? new MessageHeader() : null, routingKey: null);

            _sendToConsumers[toConsumer].WaitForConfirms();
        }

        /// <summary>
        /// Sends the given message to your consumer, you must have passed the consumer into the MicroserviceTester constructor since all adapter setup happens via option
        /// at MessageBroker construction time
        /// </summary>
        /// <param name="toConsumer"></param>
        /// <param name="header"></param>
        /// <param name="msg"></param>
        public void SendMessage(ConsumerOptions toConsumer, IMessageHeader header, IMessage msg)
        {
            _sendToConsumers[toConsumer].SendMessage(msg, header, routingKey: null);
            _sendToConsumers[toConsumer].WaitForConfirms();
        }

        /// <summary>
        /// Creates a self titled RabbitMQ exchange/queue pair where the name of the exchange is the ProducerOptions.ExchangeName and the queue has the same name.
        /// This will delete and recreate the exchange if it already exists (ensuring no old messages are stuck floating around). 
        /// </summary>
        /// <param name="exchangeName"></param>
        /// <param name="queueName"></param>
        /// <param name="isSecondaryBinding">false to create an entirely new Exchange=>Queue (including deleting any existing queue/exchange). False to simply declare the 
        /// queue and bind it to the exchange which is assumed to already exist (this allows you to set up exchange=>multiple queues).  If you are setting up multiple queues
        /// from a single exchange the first call should be isSecondaryBinding = false and all further calls after that for the same exchange should be isSecondaryBinding=true </param>
        /// <param name="routingKey"></param>
        public void CreateExchange(string exchangeName, string? queueName = null, bool isSecondaryBinding = false, string routingKey = "")
        {
            if (!exchangeName.Contains("TEST."))
                exchangeName = exchangeName.Insert(0, "TEST.");

            string queueNameToUse = queueName ?? exchangeName.Replace("Exchange", "Queue");

            using var model = Broker.GetModel(nameof(CreateExchange));
            //setup a sender channel for each of the consumers you want to test sending messages to

            //terminate any old queues / exchanges
            if (!isSecondaryBinding)
                model.ExchangeDelete(exchangeName);

            model.QueueDelete(queueNameToUse);

            //Create a binding between the exchange and the queue
            if (!isSecondaryBinding)
                model.ExchangeDeclare(exchangeName, ExchangeType.Direct, true);//durable seems to be needed because MessageBroker wants it?

            model.QueueDeclare(queueNameToUse, true, false, false); //shared with other users
            model.QueueBind(queueNameToUse, exchangeName, routingKey);

            Console.WriteLine("Created Exchange " + exchangeName + "=>" + queueNameToUse);
        }

        /// <summary>
        /// Consumes all messages from the specified queue. Must all be of type T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queueName"></param>
        /// <returns></returns>
        public IEnumerable<Tuple<IMessageHeader, T>> ConsumeMessages<T>(string queueName) where T : IMessage
        {
            IModel model = Broker.GetModel($"ConsumeMessages-{queueName}");

            while (true)
            {
                BasicGetResult message = model.BasicGet(queueName, autoAck: true);
                if (message == null)
                    break;
                var header = new MessageHeader(message.BasicProperties.Headers, Encoding.UTF8);
                var iMessage = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(message.Body.Span));
                yield return new Tuple<IMessageHeader, T>(header, iMessage);
            }
        }

        /// <summary>
        /// Shuts down the tester without performing cleanup of any declared queues / exchanges
        /// </summary>
        public void Shutdown()
        {
            foreach (MicroserviceHost host in StopOnDispose)
                host.Stop("MicroserviceTester Disposed");
        }

        /// <summary>
        /// Deletes any declared queues / exchanges depending on the CleanUpAfterTest option, then calls shutdown
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Shutdown();

            if (CleanUpAfterTest)
            {
                using IModel model = Broker.GetModel(nameof(MicroserviceTester.Dispose));
                _declaredExchanges.ForEach(x => model.ExchangeDelete(x));
                _declaredQueues.ForEach(x => model.QueueDelete(x));
            }

            Broker.Shutdown(RabbitMQBroker.DefaultOperationTimeout);
        }
    }
}
