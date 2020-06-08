
using NUnit.Framework;
using RabbitMQ.Client;
using Smi.Common.Messaging;
using Smi.Common.Options;
using System;
using System.Collections.Generic;

namespace Smi.Common.Tests.DeadLetterMessagingTests
{
    public class DeadLetterTestHelper : IDisposable
    {
        public const string RejectExchangeName = "TEST.MessageRejectExchange";
        public const string RejectQueueName = "TEST.MessageRejectQueue";
        private const string TestDlExchangeName = "TEST.DLExchange";
        public const string TestDlQueueName = "TEST.DLQueue";
        public const string TestRoutingKey = "test.routing.key";

        public GlobalOptions GlobalOptions;

        private RabbitMqAdapter _testAdapter;

        public IModel TestModel;

        public ProducerModel TestProducer;
        private ConsumerOptions _messageRejectorOptions;
        public MessageRejector MessageRejectorConsumer;
        private Guid _rejectorId;

        public bool DeleteRabbitBitsOnDispose { get; set; }


        public void SetUpSuite()
        {
            TestLogger.Setup();
            GlobalOptions = GlobalOptions.Load("default.yaml", TestContext.CurrentContext.TestDirectory);
            _testAdapter = new RabbitMqAdapter(GlobalOptions.RabbitOptions.CreateConnectionFactory(), "TestHost");

            TestModel = _testAdapter.Conn.CreateModel();
            TestModel.ConfirmSelect();

            IBasicProperties props = TestModel.CreateBasicProperties();
            props.ContentEncoding = "UTF-8";
            props.ContentType = "application/json";
            props.Persistent = true;

            TestModel.ExchangeDeclare(TestDlExchangeName, "topic", true);
            TestModel.QueueDeclare(TestDlQueueName, true, false, false);
            TestModel.QueueBind(TestDlQueueName, TestDlExchangeName, RabbitMqAdapter.RabbitMqRoutingKey_MatchAnything);

            var queueProps = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", TestDlExchangeName }
            };

            TestModel.ExchangeDeclare(RejectExchangeName, "direct", true);
            TestModel.QueueDeclare(RejectQueueName, true, false, false, queueProps);
            TestModel.QueueBind(RejectQueueName, RejectExchangeName, TestRoutingKey);

            TestModel.ExchangeDeclare(GlobalOptions.RabbitOptions.RabbitMqControlExchangeName, "topic", true);
            TestModel.ExchangeDeclare(GlobalOptions.RabbitOptions.FatalLoggingExchange, "direct", true);

            TestProducer = new ProducerModel(RejectExchangeName, TestModel, props);

            _messageRejectorOptions = new ConsumerOptions
            {
                QueueName = RejectQueueName,
                QoSPrefetchCount = 1,
                AutoAck = false
            };

            PurgeQueues();
        }

        public void ResetSuite()
        {
            PurgeQueues();

            if (_rejectorId != Guid.Empty)
                _testAdapter.StopConsumer(_rejectorId, RabbitMqAdapter.DefaultOperationTimeout);

            MessageRejectorConsumer = new MessageRejector { AcceptNext = false };
            _rejectorId = _testAdapter.StartConsumer(_messageRejectorOptions, MessageRejectorConsumer);
        }

        public void Dispose()
        {
            if (DeleteRabbitBitsOnDispose)
            {
                TestModel.QueueDelete(RejectQueueName);
                TestModel.ExchangeDelete(RejectExchangeName);
            }

            TestModel.Close();

            _testAdapter.Shutdown(RabbitMqAdapter.DefaultOperationTimeout);
        }

        private void PurgeQueues()
        {
            TestModel.QueuePurge(RejectQueueName);
            TestModel.QueuePurge(TestDlQueueName);
        }
    }
}
