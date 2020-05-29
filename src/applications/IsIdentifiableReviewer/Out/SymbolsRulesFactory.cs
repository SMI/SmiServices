using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microservices.IsIdentifiable.Reporting;

namespace IsIdentifiableReviewer.Out
{
    public class SymbolsRulesFactory : IRulePatternFactory
    {
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
                    if (char.IsDigit(cur))
                        sb.Append("\\d");
                    else
                    if (char.IsLetter(cur))
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