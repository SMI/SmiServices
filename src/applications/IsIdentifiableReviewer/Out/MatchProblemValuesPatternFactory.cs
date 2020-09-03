using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microservices.IsIdentifiable.Failures;
using Microservices.IsIdentifiable.Reporting;

namespace IsIdentifiableReviewer.Out
{
    /// <summary>
    /// <see cref="IRulePatternFactory"/> which generates <see cref="Regex"/> rule patterns that match only the <see cref="FailurePart.Word"/> and allowing anything between/before
    /// </summary>
    public class MatchProblemValuesPatternFactory: IRulePatternFactory
    {
        private MatchWholeStringRulePatternFactory _fallback = new MatchWholeStringRulePatternFactory();

        /// <summary>
        /// Returns a pattern that matches <see cref="FailurePart.Word"/> in <see cref="Failure.ProblemValue"/>.  If the word appears at the start/end of the value then ^ or $ is used.  When there are multiple failing parts anything is permitted inbweteen i.e. .*
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="failure"></param>
        /// <returns></returns>
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