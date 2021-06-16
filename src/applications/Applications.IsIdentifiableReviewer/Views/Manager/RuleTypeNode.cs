using Microservices.IsIdentifiable.Rules;
using System.Collections.Generic;

namespace IsIdentifiableReviewer.Views.Manager
{
    internal class RuleTypeNode
    {
        private string _categoryName;
        public ICustomRule[] Rules { get; internal set; }

        public RuleTypeNode(RuleSetFileNode ruleSet, string categoryName, ICustomRule[] rules)
        {
            _categoryName = categoryName;
            Rules = rules ?? new ICustomRule[0];
        }

        public override string ToString()
        {
            return _categoryName;
        }
    }
}