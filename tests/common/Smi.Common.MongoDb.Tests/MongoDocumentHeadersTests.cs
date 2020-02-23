
using MongoDB.Bson;
using NUnit.Framework;
using Smi.Common.Messages;
using System;
using System.Collections.Generic;

namespace Smi.Common.MongoDB.Tests
{
    [TestFixture]
    public class MongoDocumentHeadersTests
    {
        [Test]
        public void ImageDocumentHeader_HasCorrectHeaders()
        {
            var msg = new DicomFileMessage
            {
                DicomFilePath = "path/to/file.dcm",
                NationalPACSAccessionNumber = "1234"
            };

            string parents = $"{Guid.NewGuid().ToString()}->{Guid.NewGuid().ToString()}";
            var headers = new Dictionary<string, object>
            {
                { "MessageGuid", Guid.NewGuid().ToString() },
                { "ProducerProcessID", 1234 },
                { "ProducerExecutableName", "MongoDocumentHeadersTests" },
                { "Parents", parents },
                { "OriginalPublishTimestamp", MessageHeader.UnixTimeNow() }
            };

            var header = new MessageHeader(headers);
            BsonDocument bsonImageHeader = MongoDocumentHeaders.ImageDocumentHeader(msg, header);

            var expected = new BsonDocument
            {
                { "DicomFilePath",               msg.DicomFilePath },
                { "DicomFileSize",               msg.DicomFileSize },
                { "NationalPACSAccessionNumber", msg.NationalPACSAccessionNumber },
                { "MessageHeader", new BsonDocument
                {
                    { "MessageGuid", header.MessageGuid.ToString() },
                    { "ProducerProcessID", header.ProducerProcessID },
                    { "ProducerExecutableName", header.ProducerExecutableName },
                    { "Parents", string.Join(MessageHeader.Splitter, header.Parents) },
                    { "OriginalPublishTimestamp", header.OriginalPublishTimestamp }
                }}
            };

            Assert.AreEqual(expected, bsonImageHeader);
        }

        [Test]
        public void SeriesDocumentHeader_HasCorrectHeaders()
        {
            var msg = new SeriesMessage
            {
                DirectoryPath = "path/to/files",
                NationalPACSAccessionNumber = "AAA",
                ImagesInSeries = 1234
            };

            BsonDocument seriesHeader = MongoDocumentHeaders.SeriesDocumentHeader(msg);

            var expected = new BsonDocument
            {
                { "DirectoryPath",               msg.DirectoryPath },
                { "NationalPACSAccessionNumber", msg.NationalPACSAccessionNumber },
                { "ImagesInSeries",              msg.ImagesInSeries }
            };

            Assert.AreEqual(expected, seriesHeader);
        }

        [Test]
        public void RebuildMessageHeader_HasCorrectHeaders()
        {
            var msg = new DicomFileMessage
            {
                DicomFilePath = "path/to/file.dcm",
                NationalPACSAccessionNumber = "1234"
            };

            string parents = $"{Guid.NewGuid().ToString()}->{Guid.NewGuid().ToString()}";
            var headers = new Dictionary<string, object>
            {
                { "MessageGuid", Guid.NewGuid().ToString() },
                { "ProducerProcessID", 1234 },
                { "ProducerExecutableName", "MongoDocumentHeadersTests" },
                { "Parents", parents },
                { "OriginalPublishTimestamp", MessageHeader.UnixTimeNow() }
            };

            var header = new MessageHeader(headers);
            BsonDocument bsonImageHeader = MongoDocumentHeaders.ImageDocumentHeader(msg, header);
            IMessageHeader rebuiltHeader = MongoDocumentHeaders.RebuildMessageHeader(bsonImageHeader["MessageHeader"].AsBsonDocument);

            Assert.AreEqual(header, rebuiltHeader);
        }
    }
}
