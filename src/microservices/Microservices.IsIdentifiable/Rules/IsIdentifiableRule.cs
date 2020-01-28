using System;
using System.Text.RegularExpressions;
using Microservices.IsIdentifiable.Failures;

namespace Microservices.IsIdentifiable.Rules
{
    /// <summary>
    /// A simple Regex based rule that allows flexible white listing or blacklisting of values
    /// either in all columns or only a single column
    /// </summary>
    public class IsIdentifiableRule : ICustomRule
    {
        private Regex _ifPattern;

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

        /// <summary>
        /// The Regex pattern which should be used to match values with
        /// </summary>
        public string IfPattern
        {
            get => _ifPattern?.ToString();
            set => _ifPattern = value == null ? null : new Regex(value,RegexOptions.IgnoreCase);
        }


        public RuleAction Apply(string fieldName, string fieldValue, out FailureClassification classification,
            out int offset, out string badWord)
        {
            classification = FailureClassification.None;
            offset = 0;
            badWord = null;

            if (Action == RuleAction.None)
                return RuleAction.None;

            if(IfColumn == null && IfPattern == null)
                throw new Exception("Illegal rule setup.  You must specify either a column or a pattern (or both)");

            if(Action == RuleAction.Report && As == FailureClassification.None)
                throw new Exception("Illegal rule setup.  You must specify 'As' when Action is Report");

            //if there is no column restriction or restriction applies to the current column
            if (string.IsNullOrWhiteSpace(IfColumn) || string.Equals(IfColumn,fieldName,StringComparison.InvariantCultureIgnoreCase))
            {
                if (Action == RuleAction.Report)
                    classification = As;

                //if there is no pattern
                if (IfPattern == null)
                    return Action;

                // or the pattern matches the string we examined
                var m = _ifPattern.Match(fieldValue);
                if (m.Success)
                {
                    badWord = m.Value;
                    offset = m.Index;
                    return Action;
                }


            }

            //our rule does not apply to the current value

            return RuleAction.None;
        }
    }
}
