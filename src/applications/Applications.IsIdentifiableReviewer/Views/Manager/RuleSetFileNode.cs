using Microservices.IsIdentifiable.Rules;
using Microservices.IsIdentifiable.Runners;
using System.IO;

namespace IsIdentifiableReviewer.Views.Manager
{
    internal class RuleSetFileNode
    {
        private readonly FileInfo _file;
        private RuleSet _ruleSet;


        public RuleSetFileNode(FileInfo file)
        {
            this._file = file;
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

            var yaml = File.ReadAllText(_file.FullName);

            var deserializer = IsIdentifiableAbstractRunner.GetDeserializer();
            return _ruleSet = deserializer.Deserialize<RuleSet>(yaml);
        }

        public override string ToString()
        {
            return _file.Name;
        }
    }
}