using System.Text.RegularExpressions;
using Microservices.IsIdentifiable.Reporting;

namespace IsIdentifiableReviewer.Out
{
    public class MatchWholeStringRulePatternFactory: IRulePatternFactory
    {
        public string GetPattern(Failure failure)
        {
            return "^" + Regex.Escape(failure.ProblemValue) + "$";
        }
    }
}