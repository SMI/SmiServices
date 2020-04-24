
using System.Reflection;
using Smi.Common.Messages;
using Microservices.DeadLetterReprocessor.Execution.DeadLetterStorage;
using NLog;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using Microservices.DeadLetterReprocessor.Execution.DeadLetterStorage.MongoDocuments;
using System.Text;

namespace Microservices.DeadLetterReprocessor.Execution.DeadLetterRepublishing
{
    public class DeadLetterRepublisher
    {
        public int TotalRepublished { get; private set; }


        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        private readonly IDeadLetterStore _deadLetterStore;

        private readonly IModel _model;
        private readonly IBasicProperties _props;

        private readonly TimeSpan _confirmTimeout;

        private static readonly IEnumerable<string> _messageHeaderNames;
        private static readonly IEnumerable<string> _xDeathHeaderNames;
        

        static DeadLetterRepublisher()
        {
            _messageHeaderNames = typeof(MessageHeader).GetProperties().Select(x => x.Name);

            _xDeathHeaderNames = typeof(RabbitMqXDeathHeaders)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(fi => (fi.FieldType.IsAssignableFrom(typeof(string))))
                .Select(x => (string)x.GetValue(null));
        }

        public DeadLetterRepublisher(IDeadLetterStore deadLetterStore, IModel model)
        {
            _deadLetterStore = deadLetterStore;

            _model = model;
            _model.ConfirmSelect();

            _props = _model.CreateBasicProperties();
            _props.ContentEncoding = "UTF-8";
            _props.ContentType = "application/json";
            _props.Persistent = true;
            _props.Headers = new Dictionary<string, object>();

            if(model == null)
                throw new ArgumentNullException("model");

            _model = model;
            _model.ConfirmSelect();

            //TODO Add this to RabbitOptions & implement in RabbitAdapter
            _confirmTimeout = TimeSpan.FromMilliseconds(5000);
        }

        public void RepublishMessages(string queueFilter, bool forceReprocess = false)
        {
            TotalRepublished = 0;

            _deadLetterStore
                .GetMessagesForReprocessing(queueFilter, forceReprocess)
                .ForEach(RepublishMessage);
        }

        private void RepublishMessage(MongoDeadLetterDocument deliverArgs)
        {
            var exchangeForRepublish = deliverArgs.Props.XDeathHeaders.XFirstDeathExchange;

            try
            {
                _model.ExchangeDeclarePassive(exchangeForRepublish);
            }
            catch (OperationInterruptedException e)
            {
                // If an exchange doesn't exist, then we are forced to Fatal the host
                throw new ApplicationException("Exchange for republishing did not exist (\"" + exchangeForRepublish + "\")", e);
            }

            _props.Timestamp = new AmqpTimestamp(MessageHeader.UnixTimeNow());

            // Clear the existing headers so we can update them and re-add
            ClearMessageHeaders(_props.Headers);

            // Update the new message props with the existing data from the deliverArgs
            new MessageHeader(deliverArgs.Props.MessageHeader).Populate(_props.Headers);
            foreach (KeyValuePair<string,object> header in deliverArgs.Props.Headers)
            {
                _props.Headers.Add(header.Key, header.Value);
            }
            deliverArgs.Props.XDeathHeaders.Populate(_props.Headers);


            _model.BasicPublish(exchangeForRepublish, deliverArgs.RoutingKey, true, _props, Encoding.UTF8.GetBytes(deliverArgs.Payload.ToCharArray()));

            try
            {
                _model.WaitForConfirmsOrDie(_confirmTimeout);
            }
            catch (Exception e)
            {
                // If we can't republish a certain message, send it to graveyard
                _logger.Error(e, "WaitForConfirmsOrDie Died");
                _deadLetterStore.SendToGraveyard(deliverArgs.Props.MessageHeader.MessageGuid, "Couldn't republish", e);
                return;
            }

            _deadLetterStore.NotifyMessageRepublished(deliverArgs.Props.MessageHeader.MessageGuid);
            ++TotalRepublished;
        }

        private static void ClearMessageHeaders(IDictionary<string, object> headers)
        {
            foreach (string property in _messageHeaderNames)
                headers.Remove(property);

            foreach (string property in _xDeathHeaderNames)
                headers.Remove(property);
        }
    }
}
