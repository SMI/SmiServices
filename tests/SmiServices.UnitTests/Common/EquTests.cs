using Equ;
using NUnit.Framework;
using SmiServices.Common.Messages;
using System.Collections.Generic;

namespace SmiServices.UnitTests.Common
{
    public class EquTests
    {
        #region Fixture Methods

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestLogger.Setup();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() { }

        private class FooMessage : MemberwiseEquatable<FooMessage>, IMessage
        {
            public string? FooString { get; set; }
            public List<string>? FooList { get; set; }
            public Dictionary<string, int>? FooDict { get; set; }
        }

        #endregion

        #region Test Methods

        [SetUp]
        public void SetUp() { }

        [TearDown]
        public void TearDown() { }

        #endregion

        #region Tests

        [Test]
        public void Equals_WithEqu_HandlesDictionaries()
        {
            var m1 = new FooMessage
            {
                FooString = "study",
                FooDict = new Dictionary<string, int>
                {
                    { "foo", 1 },
                    { "bar", 2 },
                }
            };

            var m2 = new FooMessage
            {
                FooString = "study",
                FooDict = []
            };

            Assert.That(m2, Is.Not.EqualTo(m1));

            m2.FooDict.Add("bar", 2);
            m2.FooDict.Add("foo", 1);

            Assert.That(m2, Is.EqualTo(m1));
        }

        [Test]
        public void Equals_WithEqu_HandlesLists()
        {
            var m1 = new FooMessage
            {
                FooString = "study",
                FooList = ["foo", "bar"]
            };

            var m2 = new FooMessage
            {
                FooString = "study",
            };

            Assert.That(m2, Is.Not.EqualTo(m1));

            m2.FooList = ["bar", "foo"];
            Assert.That(m2, Is.Not.EqualTo(m1));

            m2.FooList = ["foo", "bar"];
            Assert.That(m2, Is.EqualTo(m1));
        }

        #endregion
    }
}
