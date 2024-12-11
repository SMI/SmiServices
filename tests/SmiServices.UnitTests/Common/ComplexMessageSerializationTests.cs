using NUnit.Framework;
using SmiServices.Common.Messages;
using SmiServices.Common.Messages.Extraction;
using SmiServices.Common.MessageSerialization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmiServices.UnitTests.Common
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
                ExtractFileMessagesDispatched = new JsonCompatibleDictionary<MessageHeader, string> { { new MessageHeader(), "dave" } },
                ExtractionDirectory = "C:\\fish",
                Modality = "CT",
                ProjectNumber = "1234-5678",
            };

            var str = Newtonsoft.Json.JsonConvert.SerializeObject(msg);
            var msg2 = JsonConvert.DeserializeObject<ExtractFileCollectionInfoMessage>(str);

            Assert.Multiple(() =>
            {
                Assert.That(msg2!.ExtractFileMessagesDispatched, Has.Count.EqualTo(1));
                Assert.That(msg2.ExtractFileMessagesDispatched.Keys.Single(), Is.Not.EqualTo(null));
            });
        }

        [Test]
        public void ExtractFileCollectionInfoMessage_WithParents()
        {
            var msg = new ExtractFileCollectionInfoMessage
            {
                ExtractionJobIdentifier = Guid.NewGuid(),
                KeyValue = "f",
                ExtractFileMessagesDispatched = [],
                ExtractionDirectory = "C:\\fish",
                Modality = "CT",
                ProjectNumber = "123",
                JobSubmittedAt = DateTime.UtcNow,
            };

            var grandparent = new MessageHeader();
            var parent = new MessageHeader(grandparent);
            var child = new MessageHeader(parent);
            msg.ExtractFileMessagesDispatched.Add(child, "dave");

            var str = Newtonsoft.Json.JsonConvert.SerializeObject(msg);
            var msg2 = JsonConvert.DeserializeObject<ExtractFileCollectionInfoMessage>(str);

            Assert.Multiple(() =>
            {
                Assert.That(msg2!.ExtractFileMessagesDispatched, Has.Count.EqualTo(1));
                Assert.That(msg2.ExtractFileMessagesDispatched.Keys.Single(), Is.Not.Null);

                Assert.That(msg2.ExtractFileMessagesDispatched.Keys.First().MessageGuid, Is.EqualTo(child.MessageGuid));
                Assert.That(msg2.ExtractFileMessagesDispatched.Keys.First().Parents, Does.Contain(parent.MessageGuid));
            });
            Assert.That(msg2.ExtractFileMessagesDispatched.Keys.First().Parents, Does.Contain(grandparent.MessageGuid));
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
                Modality = "CT",
                KeyTag = "SeriesInstanceUID",
                ExtractionIdentifiers = identifiers
            };

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(message);
            Assert.That(json, Is.Not.Null);

            var reconstructed = JsonConvert.DeserializeObject<ExtractionRequestMessage>(json);
            Assert.That(reconstructed, Is.EqualTo(message));
        }
    }
}
