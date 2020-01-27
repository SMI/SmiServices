using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Microservices.IsIdentifiable.Rules
{
    public class IsIdentifiableRule : ICustomRule
    {
        private Regex _ifPattern;
        public RuleAction Action { get; set; }
        public string IfColumn { get; set; }

        public string IfPattern
        {
            get => _ifPattern?.ToString();
            set => _ifPattern = value == null ? null : new Regex(value);
        }

        public RuleAction Apply(string fieldName, string fieldValue)
        {
            if (Action == RuleAction.None)
                return RuleAction.None;

            if(IfColumn == null && IfPattern == null)
                throw new Exception("Illegal rule setup.  You must specify either a column or a pattern (or both)");

            //if there is no column restriction or restriction applies to the current column
            if (string.IsNullOrWhiteSpace(IfColumn) || string.Equals(IfColumn,fieldName,StringComparison.InvariantCultureIgnoreCase))
            {
                //if there is no pattern or the pattern matches the string we examined
                if (IfPattern == null || _ifPattern.IsMatch(fieldValue))
                    return Action;
            }

            //our rule does not apply to the current value

            return RuleAction.None;
        }
    }
}
