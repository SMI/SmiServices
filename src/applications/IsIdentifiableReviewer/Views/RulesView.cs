using IsIdentifiableReviewer.Out;
using Microservices.IsIdentifiable.Reporting;
using Microservices.IsIdentifiable.Rules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terminal.Gui;

namespace IsIdentifiableReviewer.Views
{
    class RulesView : Window
    {
        public ReportReader CurrentReport { get; }
        public IgnoreRuleGenerator Ignorer { get; }
        public RowUpdater Updater { get; }

        private TreeView _treeView;

        public RulesView(ReportReader currentReport, IgnoreRuleGenerator ignorer, RowUpdater updater)
        {
            CurrentReport = currentReport;
            Ignorer = ignorer;
            Updater = updater;
            Modal = true;

            var lblInitialSummary = new Label($"There are {ignorer.Rules.Count} ignore rules and {updater.Rules.Count} update rules.  Current report contains {CurrentReport.Failures.Length:N0} Failures");
            Add(lblInitialSummary);

            var lblEvaluate = new Label($"Evaluate:"){Y = Pos.Bottom(lblInitialSummary)+1 };
            Add(lblEvaluate);
            
			var ruleCollisions = new Button("Rule Coverage"){
                Y = Pos.Bottom(lblEvaluate) };

            ruleCollisions.Clicked += () => EvaluateRuleCoverage();
            Add(ruleCollisions);
            
            _treeView = new TreeView(){
                Y = Pos.Bottom(ruleCollisions) + 1,
                Width = Dim.Fill(),
                Height = Dim.Fill(1)
                };
            Add(_treeView);

			var close = new Button("Close",true){
                Y = Pos.Bottom(_treeView) };

            close.Clicked += () => Quit();

			Add (close);
        }

        private void EvaluateRuleCoverage()
        {
            _treeView.ClearObjects();
            
            var colliding = new TreeNode("Colliding Rules");
            var ignore = new TreeNode("Ignore Rules Used");
            var update = new TreeNode("Update Rules Used");
            var outstanding = new TreeNode("Outstanding Failures");
            var unused = new TreeNode("Unused Rules");
            
            var allRules = Ignorer.Rules.Union(Updater.Rules).ToList();

            AddDuplicatesToTree(allRules);

            _treeView.AddObjects(new []{ colliding,ignore,update,outstanding,unused});

            foreach(Failure f in CurrentReport.Failures)
            {
                
            }


        }

        private void AddDuplicatesToTree(List<IsIdentifiableRule> allRules)
        {
            var root = new TreeNode("Duplicates");
            var children = GetDuplicates(allRules).ToArray();

            root.Children = children;
            root.Text = $"Duplicates ({children.Length})";

            _treeView.AddObject(root);
        }

        public IEnumerable<DuplicateRulesNode> GetDuplicates(IList<IsIdentifiableRule> rules)
        {
            // Find all rules that have identical patterns
            foreach(var dup in rules.Where(r=>!string.IsNullOrEmpty(r.IfPattern)).GroupBy(r=>r.IfPattern))
            {
                var duplicateRules = dup.ToArray();

                if(duplicateRules.Length > 1)
                {
                    yield return new DuplicateRulesNode(dup.Key,duplicateRules);
                }
            }
        }

        private void Quit()
        {
			Application.RequestStop ();
        }
    }
}
