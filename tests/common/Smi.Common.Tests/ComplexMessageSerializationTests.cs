using System;
using System.Collections.Generic;
using System.Linq;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.MessageSerialization;
using NUnit.Framework;
using JsonConvert = Smi.Common.MessageSerialization.JsonConvert;

namespace Smi.Common.Tests
{
    public class ComplexMessageSerializationTests
    {
        [Test]
        public void ExtractFileCollectionInfoMessage_NoParents()
        {
            var msg = new ExtractFileCollectionInfoMessage
            {
                ExtractionJobIdentifier = Guid.NewGuid(),
                KeyValue = "f",
                ExtractFileMessagesDispatched = new JsonCompatibleDictionary<MessageHeader, string> {{new MessageHeader(), "dave"}},
                ExtractionDirectory = "C:\\fish",
                ProjectNumber = "1234-5678",
            };

            var str = Newtonsoft.Json.JsonConvert.SerializeObject(msg);
            var msg2 = JsonConvert.DeserializeObject<ExtractFileCollectionInfoMessage>(str);

            Assert.AreEqual(msg2!.ExtractFileMessagesDispatched.Count,1);
            Assert.IsTrue(msg2.ExtractFileMessagesDispatched.Keys.Single() is MessageHeader);
        }

        [Test]
        public void ExtractFileCollectionInfoMessage_WithParents()
        {
            var msg = new ExtractFileCollectionInfoMessage
            {
                ExtractionJobIdentifier = Guid.NewGuid(),
                KeyValue = "f",
                ExtractFileMessagesDispatched = new JsonCompatibleDictionary<MessageHeader, string>(),
                ExtractionDirectory = "C:\\fish",
                ProjectNumber = "123",
                JobSubmittedAt = DateTime.UtcNow,
            };

            var grandparent = new MessageHeader();
            var parent = new MessageHeader(grandparent);
            var child = new MessageHeader(parent);
            msg.ExtractFileMessagesDispatched.Add(child, "dave");

            var str = Newtonsoft.Json.JsonConvert.SerializeObject(msg);
            var msg2 = JsonConvert.DeserializeObject<ExtractFileCollectionInfoMessage>(str);

            Assert.AreEqual(msg2!.ExtractFileMessagesDispatched.Count, 1);
            Assert.IsTrue(msg2.ExtractFileMessagesDispatched.Keys.Single() is MessageHeader);

            Assert.AreEqual(child.MessageGuid,msg2.ExtractFileMessagesDispatched.Keys.First().MessageGuid);
            Assert.Contains(parent.MessageGuid,msg2.ExtractFileMessagesDispatched.Keys.First().Parents);
            Assert.Contains(grandparent.MessageGuid, msg2.ExtractFileMessagesDispatched.Keys.First().Parents);
        }

        [Test]
        public void TestMessageSerialization_WithGuid()
        {
            var identifiers = new List<string>
            {
                "fish1",
                "fish2",
                "fish3",
                "fish4" 
            };

            var message = new ExtractionRequestMessage
            {
                ExtractionJobIdentifier = Guid.NewGuid(),
                ProjectNumber = "1234-5678",
                ExtractionDirectory = "C:\\fish",
                KeyTag = "SeriesInstanceUID",
                ExtractionIdentifiers = identifiers
            };

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(message);
            Assert.NotNull(json);

            var reconstructed = JsonConvert.DeserializeObject<ExtractionRequestMessage>(json);
            Assert.AreEqual(message, reconstructed);
        }
    }
}
