using Microservices.IsIdentifiable.Rules;
using Smi.Common.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;
using Terminal.Gui.Trees;

namespace IsIdentifiableReviewer.Views.Manager
{
    /// <summary>
    /// View allowing editing and viewing of all rules for both IsIdentifiable and IsIdentifiableReviewer
    /// </summary>
    class AllRulesManagerView : View, ITreeBuilder<object>
    {
        private const string Analyser = "Analyser Rules";
        private const string Reviewer = "Reviewer Rules";
        private readonly IsIdentifiableOptions _analyserOpts;
        private readonly IsIdentifiableReviewerOptions _reviewerOpts;
        private RuleDetailView detailView;

        public AllRulesManagerView(IsIdentifiableOptions analyserOpts , IsIdentifiableReviewerOptions reviewerOpts)
        {
            Width = Dim.Fill();
            Height = Dim.Fill();

            this._analyserOpts = analyserOpts;
            this._reviewerOpts = reviewerOpts;

            var tv = new TreeView<object>(this);
            tv.Width = Dim.Percent(50);
            tv.Height = Dim.Fill();
            tv.AspectGetter = NodeAspectGetter;
            tv.AddObject(Analyser);
            tv.AddObject(Reviewer);
            Add(tv);

            detailView = new RuleDetailView()
            {
                X = Pos.Right(tv),
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            Add(detailView);

            tv.SelectionChanged += Tv_SelectionChanged;
        }

        private void Tv_SelectionChanged(object sender, SelectionChangedEventArgs<object> e)
        {
            if(e.NewValue is ICustomRule r)
            {
                detailView.SetRule(r);
            }
        }

        private string NodeAspectGetter(object toRender)
        {
            if(toRender is IsIdentifiableRule basicrule)
            {
                return basicrule.IfPattern;
            }

            if (toRender is SocketRule socketRule)
            {
                return socketRule.Host + ":" + socketRule.Port;
            }
            if (toRender is WhiteListRule ignoreRule)
            {
                return ignoreRule.IfPattern ?? ignoreRule.IfPartPattern;
            }

            return toRender.ToString();
        }

        public bool SupportsCanExpand => true;

        public IsIdentifiableOptions AnalyserOpts => _analyserOpts;

        public bool CanExpand(object toExpand)
        {
            return true;
        }

        public IEnumerable<object> GetChildren(object forObject)
        {
            try
            {
                return GetChildrenImpl(forObject).ToArray();
            }
            catch (Exception ex)
            {
                // if there is an error getting children e.g. file doesn't exist, put the
                // Exception object directly into the tree
                return new object[] { ex };
            }
        }

        private IEnumerable<object> GetChildrenImpl(object forObject)
        {
            if(ReferenceEquals(forObject,Analyser))
            {
                if(!string.IsNullOrWhiteSpace(_analyserOpts.RulesDirectory))
                {
                    foreach (var f in Directory.GetFiles(_analyserOpts.RulesDirectory,"*.yaml"))
                    {
                        yield return new RuleSetFileNode(new FileInfo(f));
                    }
                }

                if (!string.IsNullOrWhiteSpace(_analyserOpts.RulesFile))
                {
                    yield return new RuleSetFileNode(new FileInfo(_analyserOpts.RulesFile));
                }
            }
            if (ReferenceEquals(forObject,Reviewer))
            {
                if(!string.IsNullOrWhiteSpace(_reviewerOpts.RedList))
                {
                    yield return new FileInfo(_reviewerOpts.RedList);
                }
                if (!string.IsNullOrWhiteSpace(_reviewerOpts.IgnoreList))
                {
                    yield return new FileInfo(_reviewerOpts.IgnoreList);
                }
            }

            if (forObject is RuleSetFileNode ruleSet)
            {
                yield return new RuleTypeNode(ruleSet, "BasicRules", ruleSet.GetRuleSet().BasicRules);
                yield return new RuleTypeNode(ruleSet, "SocketRules", ruleSet.GetRuleSet().SocketRules);
                yield return new RuleTypeNode(ruleSet, "WhiteListRules", ruleSet.GetRuleSet().WhiteListRules);
                yield return new RuleTypeNode(ruleSet, "ConsensusRules", ruleSet.GetRuleSet().ConsensusRules);                
            }

            if(forObject is RuleTypeNode ruleType)
            {
                foreach(var r in ruleType.Rules)
                {
                    yield return r;
                }
            }
        }
    }
}
