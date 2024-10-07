using NUnit.Framework;
using Rdmp.Core.Curation.Data;
using SmiServices.Microservices.DicomRelationalMapper.Namers;
using System;

namespace SmiServices.UnitTests.Microservices.DicomRelationalMapper.Namers
{
    public class GuidDatabaseNamerTests
    {
        [Test]
        public void GetExampleName()
        {
            //t6ff062af5538473f801ced2b751c7897test_RAW
            //t6ff062af5538473f801ced2b751c7897DLE_STAGING
            var namer = new GuidDatabaseNamer("test", new Guid("6ff062af-5538-473f-801c-ed2b751c7897"));

            var raw = namer.GetDatabaseName("test", LoadBubble.Raw);
            Console.WriteLine(raw);

            Assert.That(raw, Does.Contain("6ff"));

            var staging = namer.GetDatabaseName("test", LoadBubble.Staging);
            Console.WriteLine(staging);

            Assert.That(staging, Does.Contain("6ff"));
        }

    }
}
