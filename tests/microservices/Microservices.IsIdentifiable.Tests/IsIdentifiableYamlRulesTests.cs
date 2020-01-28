using System;
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
# Ignore any values in the column Modality
- Action: Ignore
  IfColumn: Modality

# Ignore the value CT in the column Modality
- Action: Ignore
  IfColumn: Modality
  IfPattern: ^CT$

# Report as an error any values which contain 2 digits
- IfPattern: ""[0-9][0-9]""
  
  As: PrivateIdentifier";

            var deserializer = new Deserializer();
            var rules = deserializer.Deserialize<IsIdentifiableRule[]>(yaml);


            Assert.AreEqual(3,rules.Length);

            Assert.AreEqual(RuleAction.Ignore,rules[0].Action);
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

            Assert.AreEqual(isReport? RuleAction.Report : RuleAction.Ignore,rule.Apply("MODALITY","CT", out FailureClassification f, out _));
            Assert.AreEqual(isReport ? FailureClassification.Date : FailureClassification.None,f);

            Assert.AreEqual(RuleAction.None,rule.Apply("ImageType","PRIMARY", out _, out _));
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

            Assert.AreEqual(isReport? RuleAction.Report : RuleAction.Ignore,rule.Apply("Modality","CT", out _, out _));
            Assert.AreEqual(RuleAction.None,rule.Apply("Modality","MR", out _, out _));
            Assert.AreEqual(RuleAction.None,rule.Apply("ImageType","PRIMARY", out _, out _));
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

            Assert.AreEqual(isReport? RuleAction.Report : RuleAction.Ignore,rule.Apply("Modality","CT", out _, out _));
            Assert.AreEqual(isReport? RuleAction.Report : RuleAction.Ignore,rule.Apply("ImageType","CT", out _, out _)); //ignore both because no restriction on column
            Assert.AreEqual(RuleAction.None,rule.Apply("ImageType","PRIMARY", out _, out _));
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

            var ex = Assert.Throws<Exception>(()=>rule.Apply("Modality","CT", out _, out _));

            Assert.AreEqual("Illegal rule setup.  You must specify either a column or a pattern (or both)",ex.Message);
        }
    }
}
