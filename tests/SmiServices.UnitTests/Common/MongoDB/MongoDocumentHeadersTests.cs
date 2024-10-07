
using MongoDB.Bson;
using NUnit.Framework;
using SmiServices.Common.Messages;
using SmiServices.Common.MongoDB;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmiServices.UnitTests.Common.MongoDB
{
    [TestFixture]
    public class MongoDocumentHeadersTests
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
        }

        [Test]
        public void ImageDocumentHeader_HasCorrectHeaders()
        {
            var msg = new DicomFileMessage
            {
                DicomFilePath = "path/to/file.dcm",
            };

            string parents = $"{Guid.NewGuid()}->{Guid.NewGuid()}";
            var headers = new Dictionary<string, object>
            {
                { "MessageGuid", Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()) },
                { "ProducerProcessID", 1234 },
                { "ProducerExecutableName", Encoding.UTF8.GetBytes("MongoDocumentHeadersTests") },
                { "Parents", Encoding.UTF8.GetBytes(parents) },
                { "OriginalPublishTimestamp", MessageHeader.UnixTimeNow() }
            };

            var header = MessageHeader.FromDict(headers, Encoding.UTF8);
            BsonDocument bsonImageHeader = MongoDocumentHeaders.ImageDocumentHeader(msg, header);

            var expected = new BsonDocument
            {
                { "DicomFilePath",               msg.DicomFilePath },
                { "DicomFileSize",               msg.DicomFileSize },
                { "MessageHeader", new BsonDocument
                {
                    { "MessageGuid", header.MessageGuid.ToString() },
                    { "ProducerProcessID", header.ProducerProcessID },
                    { "ProducerExecutableName", header.ProducerExecutableName },
                    { "Parents", string.Join(MessageHeader.Splitter, header.Parents) },
                    { "OriginalPublishTimestamp", header.OriginalPublishTimestamp }
                }}
            };

            Assert.That(bsonImageHeader, Is.EqualTo(expected));
        }

        [Test]
        public void SeriesDocumentHeader_HasCorrectHeaders()
        {
            var msg = new SeriesMessage
            {
                DirectoryPath = "path/to/files",
                ImagesInSeries = 1234
            };

            BsonDocument seriesHeader = MongoDocumentHeaders.SeriesDocumentHeader(msg);

            var expected = new BsonDocument
            {
                { "DirectoryPath",               msg.DirectoryPath },
                { "ImagesInSeries",              msg.ImagesInSeries }
            };

            Assert.That(seriesHeader, Is.EqualTo(expected));
        }

        [Test]
        public void RebuildMessageHeader_HasCorrectHeaders()
        {
            var msg = new DicomFileMessage
            {
                DicomFilePath = "path/to/file.dcm",
            };

            string parents = $"{Guid.NewGuid()}->{Guid.NewGuid()}";
            var headers = new Dictionary<string, object>
            {
                { "MessageGuid", Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()) },
                { "ProducerProcessID", 1234 },
                { "ProducerExecutableName", Encoding.UTF8.GetBytes("MongoDocumentHeadersTests") },
                { "Parents", Encoding.UTF8.GetBytes(parents) },
                { "OriginalPublishTimestamp", MessageHeader.UnixTimeNow() }
            };

            var header = MessageHeader.FromDict(headers, Encoding.UTF8);
            BsonDocument bsonImageHeader = MongoDocumentHeaders.ImageDocumentHeader(msg, header);
            IMessageHeader rebuiltHeader = MongoDocumentHeaders.RebuildMessageHeader(bsonImageHeader["MessageHeader"].AsBsonDocument);

            Assert.That(rebuiltHeader, Is.EqualTo(header));
        }
    }
}
