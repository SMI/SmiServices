using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
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
- Action: Report
  IfPattern: ""[0-9][0-9]""";

            var deserializer = new Deserializer();
            var rules = deserializer.Deserialize<IsIdentifiableRule[]>(yaml);


            Assert.AreEqual(3,rules.Length);

            Assert.AreEqual(RuleAction.Ignore,rules[0].Action);
        }

        [Test]
        public void TestOneRule_IsColumnMatch_NoPattern()
        {
            var rule = new IsIdentifiableRule()
            {
                Action = RuleAction.Ignore,
                IfColumn = "Modality"
            };

            Assert.AreEqual(RuleAction.Ignore,rule.Apply("MODALITY","CT"));
            Assert.AreEqual(RuleAction.None,rule.Apply("ImageType","PRIMARY"));
        }
        
        [Test]
        public void TestOneRule_IsColumnMatch_WithPattern()
        {
            var rule = new IsIdentifiableRule()
            {
                Action = RuleAction.Ignore,
                IfColumn = "Modality",
                IfPattern = "^CT$"
            };

            Assert.AreEqual(RuleAction.Ignore,rule.Apply("Modality","CT"));
            Assert.AreEqual(RuleAction.None,rule.Apply("Modality","MR"));
            Assert.AreEqual(RuleAction.None,rule.Apply("ImageType","PRIMARY"));
        }

        [Test]
        public void TestOneRule_NoColumn_WithPattern()
        {
            var rule = new IsIdentifiableRule()
            {
                Action = RuleAction.Ignore,
                IfPattern = "^CT$"
            };

            Assert.AreEqual(RuleAction.Ignore,rule.Apply("Modality","CT"));
            Assert.AreEqual(RuleAction.Ignore,rule.Apply("ImageType","CT")); //ignore both because no restriction on column
            Assert.AreEqual(RuleAction.None,rule.Apply("ImageType","PRIMARY"));
        }


        [Test]
        public void TestOneRule_NoColumn_NoPattern()
        {
            //rule is to ignore everything
            var rule = new IsIdentifiableRule()
            {
                Action = RuleAction.Ignore
            };

            var ex = Assert.Throws<Exception>(()=>rule.Apply("Modality","CT"));

            Assert.AreEqual("Illegal rule setup.  You must specify either a column or a pattern (or both)",ex.Message);
        }
    }
}
