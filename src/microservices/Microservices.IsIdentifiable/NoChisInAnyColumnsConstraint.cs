using System.Text.RegularExpressions;

namespace Microservices.IsIdentifiable
{
    public class NoChisInAnyColumnsConstraint
    {
        // DDMMYY + 4 digits 
        // \b bounded i.e. not more than 10 digits
        static readonly Regex _chiRegex = new(@"\b[0-3][0-9][0-1][0-9][0-9]{6}\b");

        public string GetHumanReadableDescriptionOfValidation()
        {
            return "Checks all cells in the current row for any fields containing chis";
        }

        public string? Validate(object[] otherColumns, string[] otherColumnNames)
        {
            for (var i = 0; i < otherColumnNames.Length; i++)
            {
                if (otherColumns[i] is string s && ContainsChi(s))
                    return $"Found chi in field {otherColumnNames[i]}";
            }

            return null;
        }

        private static bool ContainsChi(string value)
        {
            return _chiRegex.IsMatch(value);
        }
    }
}
