using IsIdentifiableReviewer.Out;
using Microservices.IsIdentifiable.Failures;
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
            _treeView.KeyPress += _treeView_KeyPress;

            Add(_treeView);

			var close = new Button("Close",true){
                Y = Pos.Bottom(_treeView) };

            close.Clicked += () => Quit();

			Add (close);
        }

        private void _treeView_KeyPress(KeyEventEventArgs e)
        {
            if(_treeView.HasFocus && _treeView.CanFocus)
            {
                switch(e.KeyEvent.Key)
                {
                    case Key.Enter:

                        var ofn = _treeView.SelectedObject as  OutstandingFailureNode;
                        
                        if(ofn != null)
                        {
                            var result = MessageBox.Query(MainWindow.DlgWidth,MainWindow.DlgHeight,"Action",ofn.Failure.ProblemValue,"Ignore","Update","Cancel");

                            if(result == 0)
                                Ignore(ofn);
                            if(result == 1)
                                Update(ofn);
                        }

                        e.Handled = true;
                        return;
                }
            }
        }

        private void Update(OutstandingFailureNode ofn)
        {
            Updater.Add(ofn.Failure);
        }

        private void Ignore(OutstandingFailureNode ofn)
        {
            Ignorer.Add(ofn.Failure);
        }

        private void EvaluateRuleCoverage()
        {
            _treeView.ClearObjects();
            
            var colliding = new TreeNode("Loading...");
            var ignore = new TreeNode("Loading...");
            var update = new TreeNode("Loading...");
            var outstanding = new TreeNode("Loading...");
                        
            var allRules = Ignorer.Rules.Union(Updater.Rules).ToList();

            AddDuplicatesToTree(allRules);

            _treeView.AddObjects(new []{ colliding,ignore,update,outstanding});

            Dictionary<IsIdentifiableRule,int> rulesUsed = new Dictionary<IsIdentifiableRule, int>();
            Dictionary<string,OutstandingFailureNode> outstandingFailures = new Dictionary<string, OutstandingFailureNode>();
            
            foreach(Failure f in CurrentReport.Failures)
            {
                var ignoreRule = Ignorer.Rules.FirstOrDefault(r=>r.Apply(f.ProblemField,f.ProblemValue, out _) != RuleAction.None);
                var updateRule = Updater.Rules.FirstOrDefault(r=>r.Apply(f.ProblemField,f.ProblemValue, out _) != RuleAction.None);

                // record how often each reviewer rule was used with a failure
                foreach(var r in new []{ ignoreRule,updateRule})
                    if(r != null)
                        if(!rulesUsed.ContainsKey(r))
                            rulesUsed.Add(r,1);
                        else
                            rulesUsed[r]++;

                // There are 2 conflicting rules for this input value (it should be updated and ignored!)
                if(ignoreRule != null && updateRule != null)
                {
                    var existing = colliding.Children.OfType<CollidingRulesNode>().FirstOrDefault(c=>c.Is(ignoreRule,updateRule));

                    if(existing != null)
                        existing.Add(f);
                    else
                        colliding.Children.Add(new CollidingRulesNode(ignoreRule,updateRule,f));
                }

                // input value that doesn't match any system rules yet
                if(ignoreRule == null && updateRule == null)
                {
                    if(!outstandingFailures.ContainsKey(f.ProblemValue))
                        outstandingFailures.Add(f.ProblemValue,new OutstandingFailureNode(f,1));
                    else
                        outstandingFailures[f.ProblemValue].NumberOfTimesReported++;
                }
            }

            foreach(var used in rulesUsed.Where(r=>r.Key.Action == RuleAction.Ignore).OrderByDescending(kvp=>kvp.Value))
                ignore.Children.Add(new RuleUsageNode(used.Key,used.Value));
            
            foreach(var used in rulesUsed.Where(r=>r.Key.Action == RuleAction.Report).OrderByDescending(kvp=>kvp.Value))
                update.Children.Add(new RuleUsageNode(used.Key,used.Value));

            outstanding.Children = outstandingFailures.Select(kvp=>kvp.Value).OrderByDescending(v=>v.NumberOfTimesReported).Cast<ITreeNode>().ToList();
                        
            colliding.Text = $"Colliding Rules ({colliding.Children.Count})";
            ignore.Text = $"Ignore Rules Used ({ignore.Children.Count})";
            update.Text = $"Update Rules Used ({update.Children.Count})";
            outstanding.Text = $"Outstanding Failures ({outstanding.Children.Count})";
        }

        private void AddDuplicatesToTree(List<IsIdentifiableRule> allRules)
        {
            var root = new TreeNode("Identical Rules");
            var children = GetDuplicates(allRules).ToArray();

            root.Children = children;
            root.Text = $"Identical Rules ({children.Length})";

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
