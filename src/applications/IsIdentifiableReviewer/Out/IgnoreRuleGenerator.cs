using System.IO;
using System.Linq;
using Microservices.IsIdentifiable.Reporting;
using Microservices.IsIdentifiable.Rules;

namespace IsIdentifiableReviewer.Out
{
    public class IgnoreRuleGenerator: OutBase
    {
        public const string DefaultFileName = "NewRules.yaml";

        public IgnoreRuleGenerator(FileInfo rulesFile):base(rulesFile)
        {
            
        }
        public IgnoreRuleGenerator() : this(new FileInfo(DefaultFileName))
        {
        }

        /// <summary>
        /// Adds a rule to ignore the given failure
        /// </summary>
        /// <param name="f"></param>
        public void Add(Failure f)
        {
            Add(f, RuleAction.Ignore);
        }
        
        /// <summary>
        /// When a new <paramref name="failure"/> is loaded, is it already covered by existing rules i.e. rules you are
        /// working on now that have been added since the report was generated
        /// </summary>
        /// <param name="failure"></param>
        /// <returns>true if it is novel</returns>
        public bool OnLoad(Failure failure)
        {
            //get user ot make a decision only if it is NOT covered by an existing rule
            return !IsCoveredByExistingRule(failure);
        }

    }
}
