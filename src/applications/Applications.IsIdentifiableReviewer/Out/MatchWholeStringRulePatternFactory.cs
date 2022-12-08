using System.Text.RegularExpressions;
using Microservices.IsIdentifiable.Reporting;

namespace IsIdentifiableReviewer.Out
{
    /// <summary>
    /// <see cref="IRulePatternFactory"/> that generates Regex rule patterns that match the full <see cref="Failure.ProblemValue"/> (entire cell value) only.
    /// </summary>
    public class MatchWholeStringRulePatternFactory: IRulePatternFactory
    {
        /// <summary>
        /// Returns a Regex pattern that matches the full cell value represented by the <paramref name="failure"/> exactly (with no permitted leading/trailing content)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="failure"></param>
        /// <returns></returns>
        public string GetPattern(object sender,Failure failure)
        {
            return "^" + Regex.Escape(failure.ProblemValue) + "$";
        }
    }
}
