using System;
using System.IO;
using System.Linq;
using Microservices.IsIdentifiable.Reporting;
using Microservices.IsIdentifiable.Rules;

namespace IsIdentifiableReviewer.Out
{
    /// <summary>
    /// <para>
    /// Implementation of OutBase for <see cref="RuleAction.Ignore"/>.  Base class <see cref="OutBase.Rules"/> should
    /// be interpreted as rules for detecting <see cref="Failure"/> which are false positives.
    /// </para>
    /// <para>See also:<seealso cref="RowUpdater"/></para>
    /// </summary>
    public class IgnoreRuleGenerator: OutBase
    {
        /// <summary>
        /// Default name for the false positive detection rules (for ignoring failures).  This file will be appended to as new rules are added.
        /// </summary>
        public const string DefaultFileName = "NewRules.yaml";

        /// <summary>
        /// Creates a new instance which stores rules in the <paramref name="rulesFile"/> (which will also have existing rules loaded from)
        /// </summary>
        public IgnoreRuleGenerator(FileInfo rulesFile):base(rulesFile)
        {
            
        }

        /// <summary>
        /// Creates a new instance which stores rules in the <see cref="DefaultFileName"/>
        /// </summary>
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
        /// Adds a rule to ignore the given failure using <paramref name="customPatternFactory"/> instead of the current <see cref="OutBase.RulesFactory"/> 
        /// </summary>
        /// <param name="f"></param>
        /// <param name="customPatternFactory"></param>
        public void Add(Failure f, IRulePatternFactory customPatternFactory)
        {
            Add(f, RuleAction.Ignore, customPatternFactory);
        }

        /// <summary>
        /// When a new <paramref name="failure"/> is loaded, is it already covered by existing rules i.e. rules you are
        /// working on now that have been added since the report was generated
        /// </summary>
        /// <param name="failure"></param>
        /// <param name="existingRule">The rule which already covers this failure</param>
        /// <returns>true if it is novel</returns>
        public bool OnLoad(Failure failure, out IsIdentifiableRule existingRule)
        {
            //get user ot make a decision only if it is NOT covered by an existing rule
            return !IsCoveredByExistingRule(failure,out existingRule);
        }

        
    }
}
