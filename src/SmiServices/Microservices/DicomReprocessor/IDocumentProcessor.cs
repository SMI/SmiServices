using MongoDB.Bson;

namespace SmiServices.Microservices.DicomReprocessor
{
    /// <summary>
    /// Interface for classes which process documents from MongoDb into a specific message type
    /// </summary>
    public interface IDocumentProcessor
    {
        /// <summary>
        /// Total number of documents successfully processed
        /// </summary>
        long TotalProcessed { get; }

        /// <summary>
        /// Total number of documents not successfully processed
        /// </summary>
        long TotalFailed { get; }


        /// <summary>
        /// Method called by the MongoDbReader for every document. Will be run in parallel, so be careful with updating state variables.
        /// </summary>
        /// <param name="document"></param>
        void ProcessDocument(BsonDocument document);

        /// <summary>
        /// 
        /// </summary>
        void SendMessages();

        void LogProgress();
    }
}
