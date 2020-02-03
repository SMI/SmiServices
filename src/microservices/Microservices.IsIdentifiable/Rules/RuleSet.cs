using System;
using System.Collections.Generic;
using System.Text;

namespace Microservices.IsIdentifiable.Rules
{
    public class RuleSet
    {
        public IsIdentifiableRule[] BasicRules { get; set; }
        public SocketRule[] SocketRules { get; set; }
    }
}
