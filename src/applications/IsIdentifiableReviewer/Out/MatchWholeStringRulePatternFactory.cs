using System.Text.RegularExpressions;
using Microservices.IsIdentifiable.Reporting;

namespace IsIdentifiableReviewer.Out
{
    public class MatchWholeStringRulePatternFactory: IRulePatternFactory
    {
        public string GetPattern(object sender,Failure failure)
        {
            return "^" + Regex.Escape(failure.ProblemValue) + "$";
        }
    }
}