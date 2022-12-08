using Microservices.IsIdentifiable.Reporting;
using Microservices.IsIdentifiable.Rules;
using System;
using System.Collections.Generic;
using Terminal.Gui;
using Terminal.Gui.Trees;

namespace IsIdentifiableReviewer.Views
{
    internal class CollidingRulesNode : TreeNode
    {
        /// <summary>
        /// An ignore rule that collides with the <see cref="UpdateRule"/> for certain input values
        /// </summary>
        public IsIdentifiableRule IgnoreRule {get; }

        /// <summary>
        /// An update rule that collides with the <see cref="IgnoreRule"/> for certain input values
        /// </summary>
        public IsIdentifiableRule UpdateRule {get; }
        
        /// <summary>
        /// Input failures that match both the <see cref="IgnoreRule"/> and the <see cref="UpdateRule"/>
        /// </summary>
        public List<Failure> CollideOn = new List<Failure>();

        public CollidingRulesNode(IsIdentifiableRule ignoreRule, IsIdentifiableRule updateRule, Failure f)
        {
            this.IgnoreRule = ignoreRule;
            this.UpdateRule = updateRule;
            this.CollideOn = new List<Failure>(new []{f });
        }

        public override string ToString()
        {
            return $"{IgnoreRule.IfPattern} : {UpdateRule.IfPattern} x{CollideOn.Count:N0}";
        }

        /// <summary>
        /// Adds the given failure to the list of input values that collide between these two rules
        /// </summary>
        /// <param name="f"></param>
        internal void Add(Failure f)
        {
            CollideOn.Add(f);
        }
    }
}
