using MongoDB.Bson;
using NLog;
using SmiServices.Common.Messaging;
using SmiServices.Common.Options;
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

        public TagPromotionProcessor(IProducerModel producerModel)
        {
            if (producerModel is not BatchProducerModel)
                throw new ArgumentException("producerModel must be a batch producer");
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
