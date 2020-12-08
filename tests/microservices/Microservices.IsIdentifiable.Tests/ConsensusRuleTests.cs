using Microservices.IsIdentifiable.Failures;
using Microservices.IsIdentifiable.Rules;
using Microservices.IsIdentifiable.Runners;
using Moq;
using NPOI.OpenXmlFormats.Wordprocessing;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YamlDotNet.Serialization;

namespace Microservices.IsIdentifiable.Tests
{
    class ConsensusRuleTests
    {
        [Test]
        public void NoConsensus_OnlyOneHasProblem()
        {
            var rule = new ConsensusRule()
            {
                Rules = new ICustomRule[]
                {
                    new TestRule(RuleAction.Report,new FailurePart("bb",FailureClassification.Person)),
                    new TestRule(RuleAction.Ignore),
                }
            };

            var result = rule.Apply("ff","vv",out IEnumerable<FailurePart> badParts);

            Assert.AreEqual(RuleAction.None,result);
            Assert.IsEmpty(badParts);
        }

        [TestCase(-1)]
        [TestCase(10)]
        public void Consensus_Exact(int offset)
        {
            var rule = new ConsensusRule()
            {
                Rules = new ICustomRule[]
                {
                    new TestRule(RuleAction.Report,new FailurePart("bb",FailureClassification.Person,offset)),
                    new TestRule(RuleAction.Report,new FailurePart("bb",FailureClassification.Person,offset)),
                }
            };

            var result = rule.Apply("ff","vv",out IEnumerable<FailurePart> badParts);

            Assert.AreEqual(RuleAction.Report,result);
            Assert.AreEqual(offset,badParts.Single().Offset);
        }

        [Test]
        public void Consensus_SingleOverlap()
        {
            var rule = new ConsensusRule()
            {
                Rules = new ICustomRule[]
                {
                    // Word is length 2 and begins at offset 10
                    new TestRule(RuleAction.Report,new FailurePart("ab",FailureClassification.Person,10)),
                    new TestRule(RuleAction.Report,new FailurePart("bc",FailureClassification.Person,11)),
                }
            };

            var result = rule.Apply("ff","abc is so cool",out IEnumerable<FailurePart> badParts);

            Assert.AreEqual(RuleAction.Report,result);
            Assert.AreEqual(10,badParts.Single().Offset);
            Assert.AreEqual("ab",badParts.Single().Word);
        }

        [Test]
        public void TestDeserialization()
        {
            string yaml = 
@"ConsensusRules:
    - Rules:
      - !SocketRule
          Host: 127.0.123.123
          Port: 1234
      - !SocketRule
          Host: 127.0.123.123
          Port: 567";

            
            var deserializer = IsIdentifiableAbstractRunner.GetDeserializer();
            var ruleSet = deserializer.Deserialize<RuleSet>(yaml);

            Assert.IsInstanceOf(typeof(ConsensusRule),ruleSet.ConsensusRules.Single());
            Assert.IsInstanceOf(typeof(SocketRule),ruleSet.ConsensusRules.Single().Rules[0]);
            Assert.AreEqual(1234,((SocketRule)ruleSet.ConsensusRules.Single().Rules[0]).Port);
            Assert.AreEqual(567,((SocketRule)ruleSet.ConsensusRules.Single().Rules[1]).Port);
        }

        class TestRule : ICustomRule
        {
            RuleAction _rule;
            FailurePart[] _parts;

            public TestRule(RuleAction rule, params FailurePart[] parts)
            {
                _parts = parts;
                _rule = rule;
            }

            public RuleAction Apply(string fieldName, string fieldValue, out IEnumerable<FailurePart> badParts)
            {
                badParts = _parts;

                return _rule;
            }
        }
    }
}
