using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microservices.IsIdentifiable.Failures;
using Microservices.IsIdentifiable.Reporting;
using Microservices.IsIdentifiable.Rules;
using YamlDotNet.Serialization;

namespace IsIdentifiableReviewer.Out
{
    public abstract class OutBase
    {
        protected List<IsIdentifiableRule> Rules { get;}
        public FileInfo RulesFile { get; }
        
        public IRulePatternFactory RulesFactory { get; set; } = new MatchWholeStringRulePatternFactory();

        protected OutBase(FileInfo rulesFile)
        {
            RulesFile = rulesFile;

            //no rules file yet
            if (!rulesFile.Exists)
            {
                //create it as an empty file
                using (rulesFile.Create())
                    Rules = new List<IsIdentifiableRule>();
            }
            else
            {
                var existingRules = File.ReadAllText(rulesFile.FullName);

                //empty rules file
                if(string.IsNullOrWhiteSpace(existingRules))
                    Rules = new List<IsIdentifiableRule>();
                else
                {
                    //populated rules file already existed
                    var deserializer = new Deserializer();
                    Rules = deserializer.Deserialize<List<IsIdentifiableRule>>(existingRules);
                }
            }
        }

        /// <summary>
        /// Adds a new rule (both to the <see cref="RulesFile"/> and the in memory <see cref="Rules"/> collection).
        /// </summary>
        /// <param name="f"></param>
        /// <param name="action"></param>
        protected void Add(Failure f, RuleAction action)
        {
            var rule = new IsIdentifiableRule
            {
                Action = action,
                IfColumn = f.ProblemField,
                IfPattern = RulesFactory.GetPattern(this,f),
                As = 
                    action == RuleAction.Ignore? 
                        FailureClassification.None : 
                        f.Parts.Select(p=>p.Classification).FirstOrDefault()
            };
            
            //don't add identical rules
            if (Rules.Any(r => r.AreIdentical(rule)))
                return;

            Rules.Add(rule);

            var serializer = new Serializer();
            var yaml = serializer.Serialize(new List<IsIdentifiableRule> {rule});
            File.AppendAllText(RulesFile.FullName,
                $"#{Environment.UserName} - {DateTime.Now}" + Environment.NewLine +
                yaml);
        }

        /// <summary>
        /// Returns true if there are any rules that already exactly cover the given <paramref name="failure"/>
        /// </summary>
        /// <param name="failure"></param>
        /// <returns></returns>
        protected bool IsCoveredByExistingRule(Failure failure)
        {
            //if any rule matches then we are covered by an existing rule
            return Rules.Any(r => r.Apply(failure.ProblemField, failure.ProblemValue, out _) != RuleAction.None);
        }
    }
}