using FellowOakDicom;
using NUnit.Framework;
using SmiServices.Common.Messages.Extraction;

namespace SmiServices.UnitTests.Microservices.CohortExtractor
{
    class ExtractionRequestMessageTests
    {
        [Test]
        public void Test_ConstructMessage()
        {
            var msg = new ExtractionRequestMessage
            {
                KeyTag = DicomTag.StudyInstanceUID.DictionaryEntry.Keyword
            };
            msg.ExtractionIdentifiers.Add("1.2.3");

            Assert.Multiple(() =>
            {
                Assert.That(msg.KeyTag, Is.EqualTo("StudyInstanceUID"));
                Assert.That(msg.ExtractionIdentifiers, Does.Contain("1.2.3"));
            });
        }
    }
}
