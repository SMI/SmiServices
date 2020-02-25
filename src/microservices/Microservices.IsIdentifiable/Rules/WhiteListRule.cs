using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microservices.IsIdentifiable.Failures;
using NLog;

// XXX using RegexOptions.Compiled may result in a large amount of static code
// which is never freed during garbage collection, see
// https://docs.microsoft.com/en-us/dotnet/standard/base-types/compilation-and-reuse-in-regular-expressions
// Note that the Regex Cache is not used in instance methods.

namespace Microservices.IsIdentifiable.Rules
{
    /// <summary>
    /// A simple Regex based rule that allows flexible white listing of values.
    /// Note that it implements ICustomRule but use ApplyWhiteListRule not Apply.
    /// </summary>
    public class WhiteListRule : ICustomRule
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        private Regex _ifPattern;
        private Regex _ifPartPattern;

        /// <summary>
        /// What to do if the rule is found to match the values being examined (e.g.
        /// whitelist the value or report the value as a validation failure)
        /// </summary>
        public RuleAction Action { get; set; }

        /// <summary>
        /// The column/tag in which to apply the rule.  If empty then the rule applies to all columns/tags.
        /// </summary>
        public string IfColumn { get; set; }

        /// <summary>
        /// A specific failure classification to match
        /// eg. None, PrivateIdentifier, Location, Person, Organization, Money, Percent, Date, Time, PixelText, Postcode
        /// </summary>
        public FailureClassification IfClassification { get; set; }

        /// <summary>
        /// The Regex pattern which should be used to match values with;
        /// IfPattern matches the whole field value, IfPartPattern matches the substring which raised a Failure.
        /// </summary>
        public string IfPattern
        {
            get => _ifPattern?.ToString();
            set => _ifPattern = value == null ? null : new Regex(value, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }
        public string IfPartPattern
        {
            get => _ifPartPattern?.ToString();
            set => _ifPartPattern = value == null ? null : new Regex(value, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        /// <summary>
        /// A fake method due to inheriting ICustomRule; never called.
        /// </summary>
        public RuleAction Apply(string fieldName, string fieldValue, out IEnumerable<FailurePart> badParts)
        {
            badParts = new List<FailurePart>();
            return RuleAction.None;
        }


        /// <summary>
        /// Test if this rule will whitelist the given field and failed part.
        /// Returns None if rule does not match ALL constraints otherwise returns the rule action
        /// which should be Ignore for a whitelist rule as Report is already true at this point.
        /// </summary>
        public RuleAction ApplyWhiteListRule(string fieldName, string fieldValue, FailurePart badPart)
        {
            // FailurePart has got Classification, Word and Offset
            // (we can't access ProblemField, ProblemValue from Failure).
            // eg. we get:
            //   fieldName=ProtocolName fieldValue=324-58-2995/6 part.Word=324-58-2995 class=Location
            //Console.WriteLine("WhiteListRule.Apply fieldName="+ fieldName + " fieldValue=" + fieldValue + " part.Word=" + badPart.Word + " class="+badPart.Classification);

            if(Action == RuleAction.Report)
                throw new Exception("Illegal whitelist rule setup. Action Report makes no sense.");

            // A column or field name is specified
            if (!string.IsNullOrWhiteSpace(IfColumn) && !string.Equals(IfColumn,fieldName,StringComparison.InvariantCultureIgnoreCase))
                return RuleAction.None;

            // A failure classification specified (eg. a Location or a Person)
            if ((IfClassification != FailureClassification.None) && (IfClassification != badPart.Classification))
                return(RuleAction.None);

            // A pattern to match the specific part (substring) which previously failed
            if (_ifPartPattern!=null && !_ifPartPattern.Matches(badPart.Word).Any())
                return(RuleAction.None);

            // A pattern to match the whole of the field value, not just the bit which failed
            if (_ifPattern!=null && !_ifPattern.Matches(fieldValue).Any())
                return(RuleAction.None);

            _logger.Debug("WhiteListing fieldName: "+ fieldName + " fieldValue: " + fieldValue + " part.Word: " + badPart.Word + " class: "+badPart.Classification
            + " due to rule: "
            + (_ifPattern == null ? "" : "Pattern: " + _ifPattern.ToString())
            + (IfPartPattern == null ? "" : " Part: " + IfPartPattern.ToString())
            + (IfColumn == null ? "" : " Column: " + IfColumn)
            + (IfClassification == FailureClassification.None ? "" : " Classification: " + IfClassification));

            return Action;
        }
    }
}
