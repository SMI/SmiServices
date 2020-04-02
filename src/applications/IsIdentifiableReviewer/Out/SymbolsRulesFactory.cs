using System.Text;
using System.Text.RegularExpressions;
using Microservices.IsIdentifiable.Reporting;

namespace IsIdentifiableReviewer.Out
{
    public class SymbolsRulesFactory : IRulePatternFactory
    {
        public string GetPattern(object sender, Failure failure)
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