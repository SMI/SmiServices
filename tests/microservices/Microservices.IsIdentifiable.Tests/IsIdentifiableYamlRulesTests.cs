using System;
using System.Collections.Generic;
using System.Linq;
using Microservices.IsIdentifiable.Failures;
using Microservices.IsIdentifiable.Rules;
using NUnit.Framework;
using YamlDotNet.Serialization;

namespace Microservices.IsIdentifiable.Tests
{
    class IsIdentifiableRuleTests
    {
        [Test]
        public void TestYamlDeserialization_OfRules()
        {
            string yaml = @"
BasicRules: 
  # Ignore any values in the column Modality
  - Action: Ignore
    IfColumn: Modality

  # Ignore the value CT in the column Modality
  - Action: Ignore
    IfColumn: Modality
    IfPattern: ^CT$

  # Report as an error any values which contain 2 digits
  - IfPattern: ""[0-9][0-9]""
    Action: Report
    As: PrivateIdentifier

SocketRules:   
  - Host: 127.0.123.123
    Port: 8080
 ";

            var deserializer = new Deserializer();
            var ruleSet = deserializer.Deserialize<RuleSet>(yaml);


            Assert.AreEqual(3,ruleSet.BasicRules.Length);

            Assert.AreEqual(RuleAction.Ignore,ruleSet.BasicRules[0].Action);

            
            Assert.AreEqual(1,ruleSet.SocketRules.Length);

            Assert.AreEqual("127.0.123.123",ruleSet.SocketRules[0].Host);
            Assert.AreEqual(8080,ruleSet.SocketRules[0].Port);

        }

        [TestCase(true)]
        [TestCase(false)]
        public void TestOneRule_IsColumnMatch_NoPattern(bool isReport)
        {
            var rule = new IsIdentifiableRule()
            {
                Action = isReport ? RuleAction.Report : RuleAction.Ignore,
                IfColumn = "Modality",
                As = FailureClassification.Date
            };

            Assert.AreEqual(isReport? RuleAction.Report : RuleAction.Ignore,rule.Apply("MODALITY","CT", out IEnumerable<FailurePart> bad));
            
            if(isReport)
                Assert.AreEqual(FailureClassification.Date,bad.Single().Classification);
            else
                Assert.IsEmpty(bad);

            Assert.AreEqual(RuleAction.None,rule.Apply("ImageType","PRIMARY", out _));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Test_RegexMultipleMatches(bool isReport)
        {
            var rule = new IsIdentifiableRule()
            {
                Action = isReport ? RuleAction.Report : RuleAction.Ignore,
                IfColumn = "Modality",
                IfPattern = "[0-9],",
                As = FailureClassification.Date
            };

            Assert.AreEqual(isReport? RuleAction.Report : RuleAction.Ignore,rule.Apply("MODALITY","1,2,3", out IEnumerable<FailurePart> bad));

            if (isReport)
            {
                var b = bad.ToArray();

                Assert.AreEqual(2,b.Length);

                Assert.AreEqual("1,",b[0].Word);
                Assert.AreEqual(FailureClassification.Date,b[0].Classification);
                Assert.AreEqual(0,b[0].Offset);

                Assert.AreEqual("2,",b[1].Word);
                Assert.AreEqual(FailureClassification.Date,b[1].Classification);
                Assert.AreEqual(2,b[1].Offset);
            }
            else
                Assert.IsEmpty(bad);

            Assert.AreEqual(RuleAction.None,rule.Apply("ImageType","PRIMARY", out _));
        }
        
        [TestCase(true)]
        [TestCase(false)]
        public void TestOneRule_IsColumnMatch_WithPattern(bool isReport)
        {
            var rule = new IsIdentifiableRule()
            {
                Action = isReport ? RuleAction.Report : RuleAction.Ignore,
                IfColumn = "Modality",
                IfPattern = "^CT$",
                As = FailureClassification.Date
            };

            Assert.AreEqual(isReport? RuleAction.Report : RuleAction.Ignore,rule.Apply("Modality","CT", out _));
            Assert.AreEqual(RuleAction.None,rule.Apply("Modality","MR", out _));
            Assert.AreEqual(RuleAction.None,rule.Apply("ImageType","PRIMARY", out _));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TestOneRule_NoColumn_WithPattern(bool isReport)
        {
            var rule = new IsIdentifiableRule()
            {
                Action = isReport ? RuleAction.Report : RuleAction.Ignore,
                IfPattern = "^CT$",
                As = FailureClassification.Date
            };

            Assert.AreEqual(isReport? RuleAction.Report : RuleAction.Ignore,rule.Apply("Modality","CT", out _));
            Assert.AreEqual(isReport? RuleAction.Report : RuleAction.Ignore,rule.Apply("ImageType","CT", out _)); //ignore both because no restriction on column
            Assert.AreEqual(RuleAction.None,rule.Apply("ImageType","PRIMARY", out _));
        }


        [TestCase(true)]
        [TestCase(false)]
        public void TestOneRule_NoColumn_NoPattern(bool isReport)
        {
            //rule is to ignore everything
            var rule = new IsIdentifiableRule()
            {
                Action = isReport ? RuleAction.Report : RuleAction.Ignore,
            };

            var ex = Assert.Throws<Exception>(()=>rule.Apply("Modality","CT", out _));

            Assert.AreEqual("Illegal rule setup.  You must specify either a column or a pattern (or both)",ex.Message);
        }
    }
}
