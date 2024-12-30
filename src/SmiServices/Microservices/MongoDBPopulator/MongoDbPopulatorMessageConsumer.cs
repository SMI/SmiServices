using SmiServices.Common.Messages;
using SmiServices.Common.Messaging;
using SmiServices.Common.Options;
using SmiServices.Microservices.MongoDBPopulator.Processing;
using System;

namespace SmiServices.Microservices.MongoDBPopulator
{
    public class MongoDbPopulatorMessageConsumer<T> : Consumer<T>, IMongoDbPopulatorMessageConsumer
        where T : IMessage
    {
        public ConsumerOptions ConsumerOptions { get; }

        public IMessageProcessor Processor { get; }


        private static readonly string _messageTypePrefix = typeof(T).Name + "Consumer: ";

        private bool _hasThrown;


        public MongoDbPopulatorMessageConsumer(MongoDbOptions mongoDbOptions, MongoDbPopulatorOptions populatorOptions, ConsumerOptions consumerOptions)
        {
            if (typeof(T) == typeof(DicomFileMessage))
            {
                var mongoImageAdapter = new MongoDbAdapter("ImageMessageProcessor", mongoDbOptions, populatorOptions.ImageCollection!);
                Processor = (IMessageProcessor<T>)new ImageMessageProcessor(populatorOptions, mongoImageAdapter, consumerOptions.QoSPrefetchCount, ExceptionCallback);
            }

            else if (typeof(T) == typeof(SeriesMessage))
            {
                var mongoSeriesAdapter = new MongoDbAdapter("SeriesMessageProcessor", mongoDbOptions, populatorOptions.SeriesCollection!);
                Processor = (IMessageProcessor<T>)new SeriesMessageProcessor(populatorOptions, mongoSeriesAdapter, consumerOptions.QoSPrefetchCount, ExceptionCallback);
            }

            else
                throw new ArgumentException("Message type " + typeof(T).Name + " not supported here");

            ConsumerOptions = consumerOptions;
            Logger.Debug(_messageTypePrefix + "Constructed for " + typeof(T).Name);

            Processor.OnAck += (o, a) => { Ack(a.Header, a.DeliveryTag); };
        }

        private void ExceptionCallback(Exception e)
        {
            //TODO Make this thread safe
            // Prevent both consumers throwing for the same reason (e.g. timeout)
            if (_hasThrown)
                return;

            _hasThrown = true;

            Fatal("Processor threw an exception", e);
        }

        protected override void ProcessMessageImpl(IMessageHeader header, T message, ulong tag)
        {
            // We are shutting down anyway
            if (Processor.IsStopping)
                return;

            try
            {
                ((IMessageProcessor<T>)Processor).AddToWriteQueue(message, header, tag);
            }
            catch (ApplicationException e)
            {
                // Catch specific exceptions we are aware of, any uncaught will bubble up to the wrapper in ProcessMessage

                ErrorAndNack(header, tag, "Error while processing " + typeof(T).Name, e);

                // ReSharper disable once RedundantJumpStatement
                return;
            }
        }
    }
}
