
using MongoDB.Bson;
using SmiServices.Common.Messages;
using System;


namespace SmiServices.Common.MongoDB
{
    public static class MongoDocumentHeaders
    {
        /// <summary>
        /// Generate a header for an image document
        /// </summary>
        /// <param name="message"></param>
        /// <param name="header"></param>
        /// <returns></returns>
        public static BsonDocument ImageDocumentHeader(DicomFileMessage message, IMessageHeader header)
        {
            return new BsonDocument
            {
                { "DicomFilePath",               message.DicomFilePath },
                { "DicomFileSize",               message.DicomFileSize },
                { "MessageHeader", new BsonDocument
                    {
                        { "MessageGuid", header.MessageGuid.ToString() },
                        { "ProducerProcessID", header.ProducerProcessID },
                        { "ProducerExecutableName", header.ProducerExecutableName },
                        { "Parents", string.Join(MessageHeader.Splitter, header.Parents) },
                        { "OriginalPublishTimestamp", header.OriginalPublishTimestamp }
                    }}
            };
        }

        /// <summary>
        /// Generate a header for a series document
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static BsonDocument SeriesDocumentHeader(SeriesMessage message)
        {
            return new BsonDocument
            {
                { "DirectoryPath",               message.DirectoryPath },
                { "ImagesInSeries",              message.ImagesInSeries }
            };
        }

        public static IMessageHeader RebuildMessageHeader(BsonDocument bsonDoc)
        {
            return new MessageHeader
            {
                MessageGuid = Guid.Parse(bsonDoc["MessageGuid"].AsString),
                ProducerProcessID = bsonDoc["ProducerProcessID"].AsInt32,
                ProducerExecutableName = bsonDoc["ProducerExecutableName"].AsString,
                Parents = MessageHeader.GetGuidArray(bsonDoc["Parents"].AsString),
                OriginalPublishTimestamp = bsonDoc["OriginalPublishTimestamp"].AsInt64
            };
        }
    }
}
