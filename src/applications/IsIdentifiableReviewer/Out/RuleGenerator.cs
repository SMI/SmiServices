using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microservices.IsIdentifiable.Reporting;
using Microservices.IsIdentifiable.Rules;
using YamlDotNet.Serialization;

namespace IsIdentifiableReviewer.Out
{
    class RuleGenerator
    {
        public FileInfo RulesFile { get; }

        public RuleGenerator(FileInfo rulesFile)
        {
            RulesFile = rulesFile;

            if (!rulesFile.Exists)
                rulesFile.Create();
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

            var serializer = new Serializer();

            var yaml = serializer.Serialize(new List<IsIdentifiableRule> {rule});
            File.AppendText(yaml);
        }
    }
}
