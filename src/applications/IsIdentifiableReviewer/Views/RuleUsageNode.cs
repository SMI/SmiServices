using Microservices.IsIdentifiable.Rules;
using Terminal.Gui;

namespace IsIdentifiableReviewer.Views
{
    internal class RuleUsageNode : TreeNode
    {
        public IsIdentifiableRule Rule { get; }
        public int NumberOfTimesUsed { get; }

        public RuleUsageNode(IsIdentifiableRule rule, int numberOfTimesUsed)
        {
            Rule = rule;
            NumberOfTimesUsed = numberOfTimesUsed;
        }

        public override string ToString()
        {
            return $"Pat:{Rule.IfPattern} Col:{Rule.IfColumn} x{NumberOfTimesUsed:N0}";
        }
    }
}