using Microservices.IsIdentifiable.Rules;

namespace IsIdentifiableReviewer.Out
{
    /// <summary>
    /// Record of a rule added to <see cref="OutBase"/> during the current session and the serialized <see cref="Yaml"/> that was persisted
    /// </summary>
    public class OutBaseHistory
    {
        /// <summary>
        /// The rule generated
        /// </summary>
        public IsIdentifiableRule Rule { get; }

        /// <summary>
        /// The serialized representation of the <see cref="Rule"/> (added to <see cref="OutBase.RulesFile"/>)
        /// </summary>
        public string Yaml { get; }

        /// <summary>
        /// Records a serialized <see cref="Rule"/>
        /// </summary>
        /// <param name="rule"></param>
        /// <param name="yaml"></param>
        public OutBaseHistory(IsIdentifiableRule rule, string yaml)
        {
            Rule = rule;
            Yaml = yaml;
        }
    }
}
