using Microservices.IsIdentifiable.Rules;

namespace IsIdentifiableReviewer.Out
{
    public class OutBaseHistory
    {
        public IsIdentifiableRule Rule { get; }
        public string Yaml { get; }

        public OutBaseHistory(IsIdentifiableRule rule, string yaml)
        {
            Rule = rule;
            Yaml = yaml;
        }
    }
}