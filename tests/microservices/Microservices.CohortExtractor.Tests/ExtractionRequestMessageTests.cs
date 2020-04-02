using System;
using System.Collections.Generic;
using System.Text;
using Dicom;
using NUnit.Framework;
using Smi.Common.Messages.Extraction;

namespace Microservices.CohortExtractor.Tests
{
    class ExtractionRequestMessageTests
    {
        [Test]
        public void Test_ConstructMessage()
        {
            var msg = new ExtractionRequestMessage();
            msg.KeyTag = DicomTag.StudyInstanceUID.DictionaryEntry.Keyword;
            msg.ExtractionIdentifiers.Add("1.2.3");

            Assert.AreEqual("StudyInstanceUID",msg.KeyTag);
            Assert.Contains("1.2.3",msg.ExtractionIdentifiers);
        }
    }
}
