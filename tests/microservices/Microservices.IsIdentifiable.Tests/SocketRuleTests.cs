using System;
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
                Assert.AreEqual(RuleAction.None,socketRule.HandleResponse("\0", out FailureClassification c, out int offset));
                Assert.AreEqual(FailureClassification.None,c);
                Assert.AreEqual(-1,offset);
            }
        }
        
        [Test]
        public void TestSocket_PositiveResponse()
        {
            using (var socketRule = new SocketRule())
            {
                Assert.AreEqual(RuleAction.Report,socketRule.HandleResponse("Person\010\0", out FailureClassification c, out int offset));
                Assert.AreEqual(FailureClassification.Person,c);
                Assert.AreEqual(10,offset);
            }
        }
                
        [Test]
        public void TestSocket_InvalidResponses()
        {
            using (var socketRule = new SocketRule())
            {
                var ex = Assert.Throws<Exception>(()=>socketRule.HandleResponse("Cadbury\010\0", out FailureClassification c, out int offset));
                StringAssert.Contains("'Cadbury' (expected a member of Enum FailureClassification)",ex.Message);

                ex = Assert.Throws<Exception>(()=>socketRule.HandleResponse("Person\0fish\0", out FailureClassification c, out int offset));
                StringAssert.Contains("Response was 'fish' (expected int)",ex.Message);
                
                ex = Assert.Throws<Exception>(()=>socketRule.HandleResponse("Person\0", out FailureClassification c, out int offset));
                StringAssert.Contains("Unexpected number of tokens in response from TCP client (expected '2' but got '1')",ex.Message);
            }
        }
    }
}
