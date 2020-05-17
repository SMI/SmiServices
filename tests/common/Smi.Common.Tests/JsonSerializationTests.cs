using JetBrains.Annotations;
using Newtonsoft.Json;
using NUnit.Framework;
using System;

namespace Smi.Common.Tests
{
    [TestFixture]
    public class JsonSerializationTests
    {
        private class P
        {
            [NotNull]
            public string A { get; }

            [NotNull]
            [JsonProperty] // NOTE(rkm 2020-05-16) Protected or private members can still be handled as long as they are marked with JsonProperty
            public string B { get; protected set; }

            public P(string a, string b)
            {
                A = a;
                B = b;
            }

            public override bool Equals(object? obj) => A == ((P)obj).A && B == ((P)obj).B;
            protected bool Equals(P other) => A == other.A && B == other.B;
            public override int GetHashCode() => HashCode.Combine(A, B);
            public override string ToString() => $"A={A},B={B}";
        }

        private class Q : P
        {
            public bool Y { get; set; }
            public string Z { get; protected set; }

            public Q(string a, string b, string z)
                : base(a, b)
            {
                Z = z;
            }
        }

        [Test]
        public void TestFoo()
        {
            var p = new P("a", "b");
            Assert.AreEqual(p, JsonConvert.DeserializeObject<P>(JsonConvert.SerializeObject(p)));
        }

        [Test]
        public void TestFoo2()
        {
            var q = new Q("a", "b", "z");
            Assert.AreEqual(q, JsonConvert.DeserializeObject<P>(JsonConvert.SerializeObject(q)));
        }
    }
}