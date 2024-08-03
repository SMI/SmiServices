using Smi.Common.Messaging;
using Smi.Common.Options;
using MongoDB.Bson;
using NLog;
using System;

namespace SmiServices.Microservices.DicomReprocessor
{
    /// <summary>
    /// Processes tags from documents into TagPromotionMessage(s) and publishes them to the config-defined tag promotion exchange
    /// </summary>
    public class TagPromotionProcessor : IDocumentProcessor
    {
        public long TotalProcessed { get; private set; }

        public long TotalFailed { get; private set; }


        private readonly ILogger _logger;

        private readonly DicomReprocessorOptions _options;

        private readonly IProducerModel _producerModel;
        private readonly string _reprocessingRoutingKey;


        public TagPromotionProcessor(DicomReprocessorOptions options, IProducerModel producerModel, string reprocessingRoutingKey)
        {
            _logger = LogManager.GetLogger(GetType().Name);

            _options = options;

            if (producerModel is not BatchProducerModel asBatchProducer)
                throw new ArgumentException("producerModel must be a batch producer");

            _producerModel = asBatchProducer;
            _reprocessingRoutingKey = reprocessingRoutingKey;
        }


        public void ProcessDocument(BsonDocument document)
        {
            throw new NotImplementedException();
        }

        public void SendMessages()
        {
            throw new NotImplementedException();
        }

        public void LogProgress()
        {
            throw new NotImplementedException();
        }
    }
}
