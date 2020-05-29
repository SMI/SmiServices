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

    public class SymbolsRulesFactory : IRulePatternFactory
    {
        public SymbolsRuleFactoryMode Mode { get; set; }

        /// <summary>
        /// Returns just the failing parts expressed as digits and wrapped in capture group(s) e.g. ^(\d\d-\d\d-\d\d).*([A-Z][A-Z])
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="failure"></param>
        /// <returns></returns>
        public string GetPattern(object sender, Failure failure)
        {
            StringBuilder sb = new StringBuilder();

            if (failure.HasOverlappingParts(false))
                return FullStringSymbols(sender, failure);
            
            foreach (var p in failure.Parts.Distinct().OrderBy(p=>p.Offset))
            {
                if (p.Offset == 0)
                    sb.Append("^");

                //match with capture group the given Word
                sb.Append( "(");

                foreach (char cur in p.Word)
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

                if (p.Offset + p.Word.Length == failure.ProblemValue.Length)
                    sb.Append("$");
                else
                    sb.Append(".*");
            }
            
            if(sb.Length == 0)
                throw new ArgumentException("Failure had no Parts");


            //trim last .*
            if (sb.ToString().EndsWith(".*"))
                return sb.ToString(0, sb.Length - 2);
            
            return sb.ToString();
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