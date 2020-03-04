using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microservices.IsIdentifiable.Reporting;
using Microservices.IsIdentifiable.Rules;
using YamlDotNet.Serialization;

namespace IsIdentifiableReviewer.Out
{
    class RuleGenerator
    {
        private List<IsIdentifiableRule> Rules;
        public FileInfo RulesFile { get; }
        
        public RuleGenerator(FileInfo rulesFile)
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
        /// Adds a rule to ignore the given failure
        /// </summary>
        /// <param name="f"></param>
        public void Add(Failure f,RuleAction action)
        {
            var rule = new IsIdentifiableRule();

            rule.Action = action;
            rule.IfColumn = f.ProblemField;
            rule.IfPattern = "^" + Regex.Escape(f.ProblemValue) + "$";

            //don't add identical rules
            if (Rules.Any(r => r.AreIdentical(rule)))
                return;

            Rules.Add(rule);

            var serializer = new Serializer();
            var yaml = serializer.Serialize(new List<IsIdentifiableRule> {rule});
            File.AppendAllText(RulesFile.FullName,yaml);
        }
        
        /// <summary>
        /// When a new <paramref name="failure"/> is loaded, is it already covered by existing rules i.e. rules you are
        /// working on now that have been added since the report was generated
        /// </summary>
        /// <param name="f"></param>
        /// <returns>true if it is novel</returns>
        public bool OnLoad(Failure failure)
        {
            return Rules.All(r => r.Apply(failure.ProblemField, failure.ProblemValue, out _) == RuleAction.None);
        }
    }
}
