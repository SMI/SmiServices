
using NUnit.Framework;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using System;


namespace Smi.Common.Tests
{
    public class MessageEqualityTests
    {
        [Test]
        public void TestEquals_AccessionDirectoryMessage()
        {
            var msg1 = new AccessionDirectoryMessage();
            var msg2 = new AccessionDirectoryMessage();

            Assert.That(msg2, Is.EqualTo(msg1));
            Assert.That(msg2.GetHashCode(), Is.EqualTo(msg1.GetHashCode()));

            msg1.DirectoryPath = @"c:\temp";
            msg2.DirectoryPath = @"c:\temp";

            Assert.That(msg2, Is.EqualTo(msg1));
            Assert.That(msg2.GetHashCode(), Is.EqualTo(msg1.GetHashCode()));
        }

        [Test]
        public void TestEquals_DicomFileMessage()
        {
            var msg1 = new DicomFileMessage();
            var msg2 = new DicomFileMessage();

            Assert.That(msg2, Is.EqualTo(msg1));
            Assert.That(msg2.GetHashCode(), Is.EqualTo(msg1.GetHashCode()));

            msg1.DicomDataset = "jsonified string";
            msg2.DicomDataset = "jsonified string";

            Assert.That(msg2, Is.EqualTo(msg1));
            Assert.That(msg2.GetHashCode(), Is.EqualTo(msg1.GetHashCode()));
        }

        [Test]
        public void TestEquals_SeriesMessage()
        {
            var msg1 = new SeriesMessage();
            var msg2 = new SeriesMessage();

            Assert.That(msg2, Is.EqualTo(msg1));
            Assert.That(msg2.GetHashCode(), Is.EqualTo(msg1.GetHashCode()));

            msg1.DicomDataset = "jsonified string";
            msg2.DicomDataset = "jsonified string";

            Assert.That(msg2, Is.EqualTo(msg1));
            Assert.That(msg2.GetHashCode(), Is.EqualTo(msg1.GetHashCode()));
        }


        private class FooExtractMessage : ExtractMessage { }

        [Test]
        public void Tests_ExtractMessage_Equality()
        {
            Guid guid = Guid.NewGuid();
            DateTime dt = DateTime.UtcNow;

            // TODO(rkm 2020-08-26) Swap these object initializers for proper constructors
            var msg1 = new FooExtractMessage
            {
                JobSubmittedAt = dt,
                ExtractionJobIdentifier = guid,
                ProjectNumber = "1234",
                ExtractionDirectory = "foo/bar",
                IsIdentifiableExtraction = true,
            };
            var msg2 = new FooExtractMessage
            {
                JobSubmittedAt = dt,
                ExtractionJobIdentifier = guid,
                ProjectNumber = "1234",
                ExtractionDirectory = "foo/bar",
                IsIdentifiableExtraction = true,
            };

            Assert.That(msg2, Is.EqualTo(msg1));
            Assert.That(msg2.GetHashCode(), Is.EqualTo(msg1.GetHashCode()));
        }
    }
}
