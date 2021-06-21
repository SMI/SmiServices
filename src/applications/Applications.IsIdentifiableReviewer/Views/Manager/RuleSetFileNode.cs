using Microservices.IsIdentifiable.Rules;
using Microservices.IsIdentifiable.Runners;
using System.IO;

namespace IsIdentifiableReviewer.Views.Manager
{
    internal class RuleSetFileNode
    {
        public FileInfo File { get; set; }
        private RuleSet _ruleSet;


        public RuleSetFileNode(FileInfo file)
        {
            this.File = file;
        }

        /// <summary>
        /// Clears cached results of calls to <see cref="GetRuleSet"/>
        /// </summary>
        public void ClearCache()
        {
            _ruleSet = null;
        }

        /// <summary>
        /// Opens the ruleset file and reads all rules.  This caches 
        /// </summary>
        /// <returns></returns>
        public RuleSet GetRuleSet()
        {
            if(_ruleSet != null)
            {
                return _ruleSet;
            }

            var yaml = System.IO.File.ReadAllText(File.FullName);

            var deserializer = IsIdentifiableAbstractRunner.GetDeserializer();
            return _ruleSet = deserializer.Deserialize<RuleSet>(yaml);
        }

        public override string ToString()
        {
            return File.Name;
        }
    }
}