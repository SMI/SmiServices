using System;
using System.Collections.Generic;
using System.Linq;
using Microservices.IsIdentifiable.Failures;
using Microservices.IsIdentifiable.Rules;
using NUnit.Framework;

namespace Microservices.IsIdentifiable.Tests
{
    class SocketRuleTests
    {
        [Test]
        public void TestSocket_NegativeResponse()
        {
            using (var socketRule = new SocketRule())
            {
                var bad = socketRule.HandleResponse("\0");
                Assert.IsEmpty(bad);
            }
        }
        
        [Test]
        public void TestSocket_PositiveResponse()
        {
            using (var socketRule = new SocketRule())
            {
                var bad = socketRule.HandleResponse("Person\010\0Dave\0").Single();

                Assert.AreEqual(FailureClassification.Person,bad.Classification);
                Assert.AreEqual(10,bad.Offset);
                Assert.AreEqual("Dave",bad.Word);
            }
        }
        [Test]
        public void TestSocket_TwoPositiveResponses()
        {
            using (var socketRule = new SocketRule())
            {
                var bad = socketRule.HandleResponse("Person\010\0Dave\0ORGANIZATION\00\0The University of Dundee\0").ToArray();

                Assert.AreEqual(2,bad.Length);

                Assert.AreEqual(FailureClassification.Person,bad[0].Classification);
                Assert.AreEqual(10,bad[0].Offset);
                Assert.AreEqual("Dave",bad[0].Word);

                Assert.AreEqual(FailureClassification.Organization,bad[1].Classification);
                Assert.AreEqual(0,bad[1].Offset);
                Assert.AreEqual("The University of Dundee",bad[1].Word);
            }
        }
        [Test]
        public void TestSocket_InvalidResponses()
        {
            using (var socketRule = new SocketRule())
            {
                var ex = Assert.Throws<Exception>(()=>socketRule.HandleResponse("Cadbury\010\0Cream Egg\0").ToArray());
                StringAssert.Contains("'Cadbury' (expected a member of Enum FailureClassification)",ex.Message);

                ex = Assert.Throws<Exception>(()=>socketRule.HandleResponse("Person\0fish\0Cream Egg\0").ToArray());
                StringAssert.Contains("Response was 'fish' (expected int)",ex.Message);
                
                ex = Assert.Throws<Exception>(()=>socketRule.HandleResponse("Person\0").ToArray());
                StringAssert.Contains("Expected tokens to arrive in multiples of 3 (but got '1')",ex.Message);
            }
        }
    }
}
