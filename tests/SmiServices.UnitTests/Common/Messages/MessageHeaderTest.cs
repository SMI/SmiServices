using NUnit.Framework;
using SmiServices.Common.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmiServices.UnitTests.Common.Messages
{
    [TestFixture]
    public class MessageHeaderTest
    {
        private readonly Dictionary<string, object> _testProps = new()
        {
            {"MessageGuid", Encoding.UTF8.GetBytes(Guid.NewGuid().ToString())},
            {"ProducerProcessID", 123},
            {"ProducerExecutableName", Encoding.UTF8.GetBytes("SomeOtherService")},
            {"OriginalPublishTimestamp", (long)456},
            {"Parents", Encoding.UTF8.GetBytes($"{Guid.NewGuid()}{MessageHeader.Splitter}{Guid.NewGuid()}")},
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
        public void SetUp()
        {
        }

        [TearDown]
        public void TearDown() { }

        #endregion

        #region Tests

        #endregion

        [Test]
        public void TestMessageHeader_Equality()
        {
            var h1 = MessageHeader.FromDict(_testProps, Encoding.UTF8);
            var h2 = MessageHeader.FromDict(_testProps, Encoding.UTF8);
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
            var h1 = MessageHeader.FromDict(_testProps, Encoding.UTF8);
            var h2 = MessageHeader.FromDict(_testProps, Encoding.UTF8);
            // "A hash function must have the following properties: - If two objects compare as equal, the GetHashCode() method for each object must return the same value"
            Assert.That(h2, Is.EqualTo(h1));
            Assert.That(h2.GetHashCode(), Is.EqualTo(h1.GetHashCode()));
        }

        [Test]
        public void CurrentProgramName_Unset_ThrowsException()
        {
            var original = MessageHeader.CurrentProgramName;
            MessageHeader.CurrentProgramName = null!;

            try
            {
                var exc = Assert.Throws<Exception>(() => new MessageHeader());

                Assert.That(exc.Message, Is.EqualTo("Value must be set before use"));
            }
            finally
            {
                MessageHeader.CurrentProgramName = original;
            }
        }
    }
}
