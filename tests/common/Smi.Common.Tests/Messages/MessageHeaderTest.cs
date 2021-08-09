using NUnit.Framework;
using Smi.Common.Messages;
using System;
using System.Collections.Generic;

namespace Smi.Common.Tests.Messages
{
    [TestFixture]
    public class MessageHeaderTest
    {
        private readonly Dictionary<string, object> _testProps = new Dictionary<string, object>
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

            // Test all the various flavours of equality

            Assert.AreEqual(h1, h2);
            Assert.True(Equals(h1, h2));
            Assert.True(h1.Equals(h2));
            Assert.True(h1 == h2);

            Assert.AreNotEqual(h1, h3);
            Assert.False(Equals(h1, h3));
            Assert.False(h1.Equals(h3));
            Assert.True(h1 != h3);
        }

        [Test]
        public void TestMessageHeader_GetHashCode()
        {
            var h1 = new MessageHeader(_testProps);
            var h2 = new MessageHeader(_testProps);
            // "A hash function must have the following properties: - If two objects compare as equal, the GetHashCode() method for each object must return the same value"
            Assert.AreEqual(h1, h2);
            Assert.AreEqual(h1.GetHashCode(), h2.GetHashCode());
        }
    }
}
