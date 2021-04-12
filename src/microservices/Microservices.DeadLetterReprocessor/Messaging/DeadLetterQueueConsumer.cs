
using Smi.Common.Messages;
using Smi.Common.Messaging;
using Smi.Common.Options;
using Microservices.DeadLetterReprocessor.Execution.DeadLetterStorage;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microservices.DeadLetterReprocessor.Messaging
{
    public class DeadLetterQueueConsumer : Consumer<IMessage>
    {
        private readonly IDeadLetterStore _deadLetterStore;
        private readonly string _deadLetterQueueName;

        private readonly int _maxRetryLimit;
        private readonly TimeSpan _defaultRetryAfter;

        private readonly Queue<Tuple<BasicDeliverEventArgs, IMessageHeader>> _storageQueue =
            new Queue<Tuple<BasicDeliverEventArgs, IMessageHeader>>();
        private readonly object _oQueueLock = new object();

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private Task _storageQueueTask;

        private bool _stopCalled;


        public DeadLetterQueueConsumer(IDeadLetterStore deadLetterStore, DeadLetterReprocessorOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.DeadLetterConsumerOptions.QueueName))
                throw new ArgumentException("DeadLetterQueueName");

            if (options.MaxRetryLimit < 1)
                throw new ArgumentException("MaxRetryLimit");

            if (options.DefaultRetryAfter < 1)
                throw new ArgumentException("DefaultRetryAfter");

            _deadLetterStore = deadLetterStore;
            _deadLetterQueueName = options.DeadLetterConsumerOptions.QueueName;
            _maxRetryLimit = options.MaxRetryLimit;
            _defaultRetryAfter = TimeSpan.FromMinutes(options.DefaultRetryAfter);

            //TODO
            //StartStorageTask(TimeSpan.FromMinutes(options.DefaultRetryAfter));
        }


        public bool MessagesInQueue()
        {
            return Model.MessageCount(_deadLetterQueueName) > 0;
        }

        public void Stop()
        {
            //TODO(RKM 04/07) Why is this here?
            return;

            Logger.Info("Stop called");

            if (_stopCalled)
            {
                Logger.Warn("Stop called twice");
                return;
            }

            _stopCalled = true;

            _cancellationTokenSource.Cancel();
            _storageQueueTask.Wait();
        }

        public override void ProcessMessage(BasicDeliverEventArgs deliverArgs)
        {
            Encoding enc = Encoding.UTF8;
            MessageHeader header;

            try
            {
                if (deliverArgs.BasicProperties.ContentEncoding != null)
                    enc = Encoding.GetEncoding(deliverArgs.BasicProperties.ContentEncoding);

                header = new MessageHeader(deliverArgs.BasicProperties.Headers, enc);
                header.Log(Logger, NLog.LogLevel.Trace, "Received");
            }
            catch (Exception e)
            {
                Logger.Error("Message header content was null, or could not be parsed into a MessageHeader object: " + e);

                DiscardSingleMessage(deliverArgs.DeliveryTag);

                return;
            }

            //Bug: RabbitMQ lib doesn't properly handle the ReplyTo address being null, causing the mapping to MongoDB types to throw an exception
            if (deliverArgs.BasicProperties.ReplyTo == null)
                deliverArgs.BasicProperties.ReplyTo = "";

            RabbitMqXDeathHeaders deathHeaders;

            try
            {
                deathHeaders = new RabbitMqXDeathHeaders(deliverArgs.BasicProperties.Headers, Encoding.UTF8);
            }
            catch (ArgumentException)
            {
                _deadLetterStore.SendToGraveyard(deliverArgs, header, "Message contained invalid x-death entries");

                Ack(header, deliverArgs.DeliveryTag);
                return;
            }

            if (deathHeaders.XDeaths[0].Count - 1 >= _maxRetryLimit)
            {
                _deadLetterStore.SendToGraveyard(deliverArgs, header, "MaxRetryCount exceeded");

                Ack(header, deliverArgs.DeliveryTag);
                return;
            }

            try
            {
                _deadLetterStore.PersistMessageToStore(deliverArgs, header, _defaultRetryAfter);
            }
            catch (Exception e)
            {
                _deadLetterStore.SendToGraveyard(deliverArgs, header, "Exception when storing message", e);
            }

            Ack(header, deliverArgs.DeliveryTag);
        }

        // NOTE(jas 2020-05-12) Dummy - actual code inlined above
        protected override void ProcessMessageImpl(IMessageHeader header, IMessage message, ulong tag) => throw new NotImplementedException("DeadLetterQueueConsumer does not implement ProcessMessageImpl");


        private void StartStorageTask(TimeSpan retryAfter)
        {
            _storageQueueTask = Task.Factory.StartNew(() =>
           {
               while (!_cancellationTokenSource.IsCancellationRequested)
               {
                   try
                   {
                       lock (_oQueueLock)
                       {
                           foreach (Tuple<BasicDeliverEventArgs, IMessageHeader> _ in _storageQueue)
                           {

                           }
                       }

                       Thread.Sleep(1000);
                   }
                   catch (Exception e)
                   {
                       Fatal("Error in storage task", e);
                       Stop();

                       break;
                   }
               }

               Logger.Debug("Storage task exiting");
           });

            Logger.Debug("Storage task started");
        }
    }
}
