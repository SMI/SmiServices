using System;
using System.Collections.Generic;
using System.Text;

namespace Microservices.IsIdentifiable.Rules
{
    public class RuleSet
    {
        public List<IsIdentifiableRule> BasicRules { get; set; } = new List<IsIdentifiableRule>();
        public List<SocketRule> SocketRules { get; set; } = new List<SocketRule>();
        public List<WhiteListRule> WhiteListRules { get; set; } = new List<WhiteListRule>();
        public List<ConsensusRule> ConsensusRules { get; set; } = new List<ConsensusRule>();
    }
}
