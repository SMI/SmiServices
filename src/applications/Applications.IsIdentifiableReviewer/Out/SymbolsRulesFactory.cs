using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microservices.IsIdentifiable.Reporting;

namespace IsIdentifiableReviewer.Out
{
    /// <summary>
    /// Determines which bits of a failure get converted to corresponding symbols
    /// </summary>
    public enum SymbolsRuleFactoryMode
    {
        /// <summary>
        /// Generates rules that match characters [A-Z]/[a-z] (depending on capitalization of input string) and digits \d
        /// </summary>
        Full,
        /// <summary>
        /// Generates rules that match any digits using \d
        /// </summary>
        DigitsOnly,

        /// <summary>
        /// Generates rules that match any characters with [A-Z]/[a-z] (depending on capitalization of input string)
        /// </summary>
        CharactersOnly
    }

    /// <summary>
    /// Generates Regex patterns for matching <see cref="Failure"/> based on permutations of digits (\d) and/or characters([A-Z] or [a-z]).  See also <seealso cref="SymbolsRuleFactoryMode"/>.
    /// </summary>
    public class SymbolsRulesFactory : IRulePatternFactory
    {
        /// <summary>
        /// Whether to generate Regex match patterns using the permutation of characters, digits or both.
        /// </summary>
        public SymbolsRuleFactoryMode Mode { get; set; }

        /// <summary>
        /// Returns just the failing parts expressed as digits and wrapped in capture group(s) e.g. ^(\d\d-\d\d-\d\d).*([A-Z][A-Z])
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="failure"></param>
        /// <returns></returns>
        public string GetPattern(object sender, Failure failure)
        {
            // failures should really have parts!
            if (!failure.Parts.Any())
                throw new ArgumentException("Failure had no Parts");

            StringBuilder sb = new StringBuilder();

            var minOffset = failure.Parts.Min(p => p.Offset);
            var maxPartEnding = failure.Parts.Max(p => p.Offset + p.Word.Length);

            if (minOffset == 0)
                sb.Append("^");

            foreach (var p in failure.ConflateParts())
            {

                //match with capture group the given Word
                sb.Append( "(");

                foreach (char cur in p)
                {
                    if (char.IsDigit(cur) && Mode != SymbolsRuleFactoryMode.CharactersOnly)
                        sb.Append("\\d");
                    else
                    if (char.IsLetter(cur) && Mode != SymbolsRuleFactoryMode.DigitsOnly)
                        sb.Append(char.IsUpper(cur) ? "[A-Z]" : "[a-z]");
                    else
                        sb.Append(Regex.Escape(cur.ToString()));
                }
                
                sb.Append(")");


                sb.Append(".*");
            }

            // If there is a failure part that ends at the end of the input string then the pattern should have a terminator
            // to denote that we only care about problem values ending in this pattern (user can always override that decision)
            if (maxPartEnding == failure.ProblemValue.Length)
            {
                return sb.ToString(0, sb.Length - 2) + '$';
            }
            else
            {
                return sb.ToString(0, sb.Length - 2);
            }
        }

        /// <summary>
        /// Returns a full symbols match of the entire input string (ProblemValue)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="failure"></param>
        /// <returns></returns>
        private string FullStringSymbols(object sender, Failure failure)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < failure.ProblemValue.Length; i++)
            {
                char cur = failure.ProblemValue[i];

                if (char.IsDigit(cur))
                    sb.Append("\\d");
                else
                if (char.IsLetter(cur))
                    sb.Append(char.IsUpper(cur) ? "[A-Z]" : "[a-z]");
                else
                {
                    sb.Append(Regex.Escape(cur.ToString()));
                }
            }

            return "^" + sb + "$";
        }
    }
}
