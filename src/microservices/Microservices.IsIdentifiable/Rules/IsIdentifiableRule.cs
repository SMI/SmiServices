using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microservices.IsIdentifiable.Failures;

// XXX using RegexOptions.Compiled may result in a large amount of static code
// which is never freed during garbage collection, see
// https://docs.microsoft.com/en-us/dotnet/standard/base-types/compilation-and-reuse-in-regular-expressions
// Note that the Regex Cache is not used in instance methods.

namespace Microservices.IsIdentifiable.Rules
{
    /// <summary>
    /// A simple Regex based rule that allows flexible white listing or blacklisting of values
    /// either in all columns or only a single column
    /// </summary>
    public class IsIdentifiableRule : ICustomRule
    {

        /// <summary>
        /// What to do if the rule is found to match the values being examined (e.g.
        /// whitelist the value or report the value as a validation failure)
        /// </summary>
        public RuleAction Action { get; set; }

        /// <summary>
        /// The column/tag in which to apply the rule.  If empty then the rule applies to all columns
        /// </summary>
        public string IfColumn { get; set; }

        /// <summary>
        /// What you are trying to classify (if <see cref="Action"/> is <see cref="RuleAction.Report"/>)
        /// </summary>
        public FailureClassification As { get; set; }
        
        protected Regex IfPatternRegex;
        private string _ifPatternString;
        private bool _caseSensitive;

        /// <summary>
        /// The Regex pattern which should be used to match values with
        /// </summary>
        public string IfPattern
        {
            get => _ifPatternString;
            set
            {
                _ifPatternString = value;
                RebuildRegex();
            }
        }

        /// <summary>
        /// Whether the IfPattern match is case sensitive (default is false)
        /// </summary>
        public virtual bool CaseSensitive
        {
            get => _caseSensitive;
            set
            {
                _caseSensitive = value;
                RebuildRegex();
            }
        }

        private void RebuildRegex()
        {
            IfPatternRegex = _ifPatternString == null ? null : new Regex(_ifPatternString, (CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase) | RegexOptions.Compiled);
        }

        public virtual RuleAction Apply(string fieldName, string fieldValue, out IEnumerable<FailurePart> badParts)
        {
            badParts = new List<FailurePart>();

            if (Action == RuleAction.None)
                return RuleAction.None;

            if(IfColumn == null && IfPattern == null)
                throw new Exception("Illegal rule setup.  You must specify either a column or a pattern (or both)");

            if(Action == RuleAction.Report && As == FailureClassification.None)
                throw new Exception("Illegal rule setup.  You must specify 'As' when Action is Report");

            //if there is no column restriction or restriction applies to the current column
            if (string.IsNullOrWhiteSpace(IfColumn) || string.Equals(IfColumn,fieldName,StringComparison.InvariantCultureIgnoreCase))
            {
                //if there is no pattern
                if (IfPattern == null)
                {
                    //we are reporting everything in this column? ok fair enough (no pattern just column name)
                    if (Action == RuleAction.Report) 
                        ((IList) badParts).Add(new FailurePart(fieldValue, As, 0));

                    return Action;
                }
                    
                // if the pattern matches the string we examined
                var matches = IfPatternRegex.Matches(fieldValue);
                if (matches.Any())
                {
                    //if we are reporting all failing regexes
                    if(Action == RuleAction.Report)
                        foreach (Match match in matches)
                            ((IList) badParts).Add(new FailurePart(match.Value, As, match.Index));

                    return Action;
                }


            }

            //our rule does not apply to the current value

            return RuleAction.None;
        }

        public bool AreIdentical(IsIdentifiableRule other)
        {
            return
                string.Equals(IfColumn, other.IfColumn,StringComparison.CurrentCultureIgnoreCase) &&
                Action == other.Action &&
                string.Equals(IfPattern, other.IfPattern,StringComparison.CurrentCultureIgnoreCase);
        }
    }
}
