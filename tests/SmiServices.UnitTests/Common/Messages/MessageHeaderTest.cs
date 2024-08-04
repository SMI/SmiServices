using NUnit.Framework;
using SmiServices.Common.Messages;
using System;
using System.Collections.Generic;

namespace SmiServices.UnitTests.Common.Messages
{
    [TestFixture]
    public class MessageHeaderTest
    {
        private readonly Dictionary<string, object> _testProps = new()
        {
            {"MessageGuid", Guid.NewGuid().ToString()},
            {"ProducerProcessID", 123},
            {"ProducerExecutableName", "Testeroni"},
            {"OriginalPublishTimestamp", (long)456},
            {"Parents", $"{Guid.NewGuid()}{MessageHeader.Splitter}{Guid.NewGuid()}"},
        };

        #region Fixture Methods 

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestLogger.Setup();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() { }

        #endregion

        #region Test Methods

        [SetUp]
        public void SetUp() { }

        [TearDown]
        public void TearDown() { }

        #endregion

        #region Tests

        #endregion

        [Test]
        public void TestMessageHeader_Equality()
        {
            var h1 = new MessageHeader(_testProps);
            var h2 = new MessageHeader(_testProps);
            var h3 = new MessageHeader();

            Assert.Multiple(() =>
            {
                // Test all the various flavours of equality

                Assert.That(h2, Is.EqualTo(h1));
                Assert.That(h1, Is.EqualTo(h2));

                Assert.That(h3, Is.Not.EqualTo(h1));
            });
            Assert.That(h1, Is.Not.EqualTo(h3));
        }

        [Test]
        public void TestMessageHeader_GetHashCode()
        {
            var h1 = new MessageHeader(_testProps);
            var h2 = new MessageHeader(_testProps);
            // "A hash function must have the following properties: - If two objects compare as equal, the GetHashCode() method for each object must return the same value"
            Assert.That(h2, Is.EqualTo(h1));
            Assert.That(h2.GetHashCode(), Is.EqualTo(h1.GetHashCode()));
        }
    }
}
