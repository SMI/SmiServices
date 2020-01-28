namespace Microservices.IsIdentifiable.Rules
{
    public enum RuleAction
    {
        /// <summary>
        /// Do not undertake any action e.g. if the rule does not apply to a given value
        /// </summary>
        None,

        /// <summary>
        /// The value should be whitelisted and ignored by any downstream classifiers that might
        /// otherwise have an issue with it
        /// </summary>
        Ignore,

        /// <summary>
        /// The value violates system rules and likely contains identifiable data.  It should be reported
        /// as a failure.
        /// </summary>
        Report
    }
}