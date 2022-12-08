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
    /// <summary>
    /// Abstract base for classes who act upon <see cref="Failure"/> by creating new <see cref="Rules"/> and/or redacting the database.
    /// </summary>
    public abstract class OutBase
    {
        /// <summary>
        /// Existing rules which describe how to detect a <see cref="Failure"/> that should be handled by this class.  These are synced with the contents of the <see cref="RulesFile"/>
        /// </summary>
        public List<IsIdentifiableRule> Rules { get;}

        /// <summary>
        /// Persistence of <see cref="RulesFile"/>
        /// </summary>
        public FileInfo RulesFile { get; }
        
        /// <summary>
        /// Factory for creating new <see cref="Rules"/> when encountering novel <see cref="Failure"/> that do not match any existing rules.  May involve user input.
        /// </summary>
        public IRulePatternFactory RulesFactory { get; set; } = new MatchWholeStringRulePatternFactory();

        /// <summary>
        /// Record of changes to <see cref="Rules"/> (and <see cref="RulesFile"/>).
        /// </summary>
        public Stack<OutBaseHistory> History = new Stack<OutBaseHistory>();

        /// <summary>
        /// Creates a new instance, populating <see cref="Rules"/> with the files serialized in <paramref name="rulesFile"/>
        /// </summary>
        /// <param name="rulesFile">Location to load/persist rules from/to.  Will be created if it does not exist yet</param>
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
                    Rules = deserializer.Deserialize<List<IsIdentifiableRule>>(existingRules) ?? new List<IsIdentifiableRule>();
                }
            }
        }

        /// <summary>
        /// Adds a new rule (both to the <see cref="RulesFile"/> and the in memory <see cref="Rules"/> collection).
        /// </summary>
        /// <param name="f"></param>
        /// <param name="action"></param>
        /// <param name="overrideRuleFactory">Overrides the current <see cref="RulesFactory"/> and uses this instead</param>
        /// <returns>The new / existing rule that covers failure</returns>
        protected IsIdentifiableRule Add(Failure f, RuleAction action,IRulePatternFactory overrideRuleFactory = null)
        {
            var factory = overrideRuleFactory ?? RulesFactory;

            var rule = new IsIdentifiableRule
            {
                Action = action,
                IfColumn = f.ProblemField,
                IfPattern = factory.GetPattern(this,f),
                As = 
                    action == RuleAction.Ignore? 
                        FailureClassification.None : 
                        f.Parts.Select(p=>p.Classification).FirstOrDefault()
            };
            
            //don't add identical rules
            if (Rules.Any(r => r.AreIdentical(rule)))
                return rule;

            Rules.Add(rule);

            var contents = Serialize(rule,true);

            File.AppendAllText(RulesFile.FullName,contents);
            History.Push(new OutBaseHistory(rule,contents));

            return rule;
        }

        /// <summary>
        /// Serializes the <paramref name="rule"/> into yaml optionally with a comment at the start
        /// </summary>
        /// <param name="rule"></param>
        /// <param name="addCreatorComment"></param>
        /// <returns></returns>
        private string Serialize(IsIdentifiableRule rule, bool addCreatorComment)
        {
            var serializer = new SerializerBuilder()
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
                .Build();
            
            string yaml = serializer.Serialize(new List<IsIdentifiableRule> {rule});
            
            if(!addCreatorComment)
                return yaml;

            return $"#{Environment.UserName} - {DateTime.Now}" + Environment.NewLine +
                           yaml;
        }

        /// <summary>
        /// Removes an existing rule and flushes out to disk
        /// </summary>
        /// <param name="rule"></param>
        /// <returns>True if the rule existed and was successfully deleted in memory and on disk</returns>
        public bool Delete(IsIdentifiableRule rule)
        {
            return Rules.Remove(rule) && Purge(Serialize(rule,false),$"# Rule deleted by {Environment.UserName} - {DateTime.Now}{Environment.NewLine}");
        }

        /// <summary>
        /// Removes the last <see cref="History"/> entry from the <see cref="Rules"/> and <see cref="RulesFile"/>.
        /// </summary>
        public void Undo()
        {
            if(History.Count == 0)
                return;

            var popped = History.Pop();

            if (popped != null)
            {
                Purge(popped.Yaml);
                
                //clear the rule from memory
                Rules.Remove(popped.Rule);
            }
        }

        /// <summary>
        /// Serializes the current <see cref="Rules"/> to the provided file
        /// </summary>
        /// <param name="toFile"></param>
        public void Save(FileInfo toFile = null)
        {
            if (toFile == null)
            {
                toFile = RulesFile;
            }

            //populated rules file already existed
            var builder = new SerializerBuilder()
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
                .WithIndentedSequences();
            
            var serializer = builder.Build();
            using (var sw = new StreamWriter(toFile.FullName))
            {
                if(Rules.Count > 0)
                {
                    serializer.Serialize(sw, Rules);
                }
                // else we get a blank file which is good because the append rules wouldn't play nice
                // with an [] deal
            }
        }

        /// <summary>
        /// Attempts to purge the provided block of serialized rules yaml from the rules base on disk
        /// </summary>
        /// <param name="yaml"></param>
        /// <param name="replacement"></param>
        /// <returns></returns>
        private bool Purge(string yaml, string replacement = "")
        {
            //clear the rule from the serialized text file
            var oldText = File.ReadAllText(RulesFile.FullName);
            var newText = oldText.Replace(yaml, replacement);

            if(Equals(oldText,newText))
                return false;

            //write to a new temp file
            File.WriteAllText(RulesFile.FullName + ".tmp",newText);

            //then hot swap them using in-place replacement added in .Net 3.0
            File.Move(RulesFile.FullName + ".tmp", RulesFile.FullName, true);
            
            return true;
        }

        /// <summary>
        /// Returns true if there are any rules that already exactly cover the given <paramref name="failure"/>
        /// </summary>
        /// <param name="failure"></param>
        /// <param name="match">The first rule that matches the <paramref name="failure"/></param>
        /// <returns></returns>
        protected bool IsCoveredByExistingRule(Failure failure, out IsIdentifiableRule match)
        {
            //if any rule matches then we are covered by an existing rule
            match = Rules.FirstOrDefault(r => r.Apply(failure.ProblemField, failure.ProblemValue, out _) != RuleAction.None);
            return match != null;
        }
    }
}
