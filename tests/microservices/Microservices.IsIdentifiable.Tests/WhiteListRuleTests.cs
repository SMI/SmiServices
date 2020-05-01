using System;
using System.Collections.Generic;
using System.Text;
using Microservices.IsIdentifiable.Failures;
using Microservices.IsIdentifiable.Rules;
using NUnit.Framework;

namespace Microservices.IsIdentifiable.Tests
{
    class WhiteListRuleTests
    {

        [Test]
        public void TestWhiteListRule_IfPattern_CaseSensitivity()
        {
            var rule = new WhiteListRule();

            //Ignore any failure where any of the input string matches "fff"
            rule.IfPattern = "fff";
            Assert.IsFalse(rule.CaseSensitive);

            Assert.AreEqual(
                RuleAction.Ignore,rule.ApplyWhiteListRule("aba", "FFF Troll", new FailurePart("Troll", FailureClassification.Location, 0)));

            rule.CaseSensitive = true;
            
            Assert.AreEqual(
                RuleAction.None,rule.ApplyWhiteListRule("aba", "FFF Troll", new FailurePart("Troll", FailureClassification.Location, 0)));
        }

        [Test]
        public void TestWhiteListRule_IfPartPattern_CaseSensitivity()
        {
            var rule = new WhiteListRule();

            //Ignore any failure the specific section that is bad is this:
            rule.IfPartPattern = "^troll$";
            Assert.IsFalse(rule.CaseSensitive);

            Assert.AreEqual(
                RuleAction.Ignore,rule.ApplyWhiteListRule("aba", "FFF Troll", new FailurePart("Troll", FailureClassification.Location, 0)));

            rule.CaseSensitive = true;
            
            Assert.AreEqual(
                RuleAction.None,rule.ApplyWhiteListRule("aba", "FFF Troll", new FailurePart("Troll", FailureClassification.Location, 0)));
        }
        
        [Test]
        public void TestWhiteListRule_As()
        {
            var rule = new WhiteListRule();

            //Ignore any failure the specific section that is bad is this:
            rule.IfPartPattern = "^troll$";
            rule.As = FailureClassification.Person;
            
            
            Assert.AreEqual(
                RuleAction.None,rule.ApplyWhiteListRule("aba", "FFF Troll",
                    new FailurePart("Troll", FailureClassification.Location, 0)),"Rule should not apply when FailureClassification is Location");
            
            Assert.AreEqual(
                RuleAction.Ignore,rule.ApplyWhiteListRule("aba", "FFF Troll",
                    new FailurePart("Troll", FailureClassification.Person, 0)),"Rule SHOULD apply when FailureClassification matches As");

        }

        [Test]
        public void TestCombiningPatternAndPart()
        {
            var rule = new WhiteListRule()
            {
                IfPartPattern = "^Brian$",
                IfPattern = "^MR Brian And Skull$"
            };

            Assert.AreEqual(
                RuleAction.Ignore,rule.ApplyWhiteListRule("aba", "MR Brian And Skull",
                    new FailurePart("Brian", FailureClassification.Person, 0)),"Rule matches on both patterns");

            Assert.AreEqual(
                RuleAction.None,rule.ApplyWhiteListRule("aba", "MR Brian And Skull",
                    new FailurePart("Skull", FailureClassification.Person, 0)),"Rule does not match on both whole string AND part so should not be ignored");

            Assert.AreEqual(
                RuleAction.None,rule.ApplyWhiteListRule("aba", "MR Brian And Skull Dr Fisher",
                    new FailurePart("Brian", FailureClassification.Person, 0)),"Rule does not match on both whole string AND part so should not be ignored");
        }


    }
}
