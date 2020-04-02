using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microservices.IsIdentifiable.Reporting;

namespace IsIdentifiableReviewer.Out
{
    public class MatchProblemValuesPatternFactory: IRulePatternFactory
    {
        private MatchWholeStringRulePatternFactory _fallback = new MatchWholeStringRulePatternFactory();

        public string GetPattern(object sender, Failure failure)
        {
            StringBuilder sb = new StringBuilder();

            if (failure.HasOverlappingParts(false))
                return _fallback.GetPattern(sender,failure);
            
            foreach (var p in failure.Parts.Distinct().OrderBy(p=>p.Offset))
            {
                if (p.Offset == 0)
                    sb.Append("^");

                //match with capture group the given Word
                sb.Append( "(" +Regex.Escape(p.Word) + ")");

                if (p.Offset + p.Word.Length == failure.ProblemValue.Length)
                    sb.Append("$");
                else
                    sb.Append(".*");
            }

            //trim last .*
            if (sb.ToString().EndsWith(".*"))
                return sb.ToString(0, sb.Length - 2);
            
            return sb.ToString();
        }
    }
}