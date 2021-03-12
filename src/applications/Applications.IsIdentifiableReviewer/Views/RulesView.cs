using IsIdentifiableReviewer.Out;
using Microservices.IsIdentifiable.Failures;
using Microservices.IsIdentifiable.Reporting;
using Microservices.IsIdentifiable.Rules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Terminal.Gui;

namespace IsIdentifiableReviewer.Views
{
    class RulesView : Window
    {
        public ReportReader CurrentReport { get; }
        public IgnoreRuleGenerator Ignorer { get; }
        public RowUpdater Updater { get; }

        private TreeView _treeView;

        /// <summary>
        /// When the user bulk ignores many records at once how should the ignore patterns be generated
        /// </summary>
        private readonly IRulePatternFactory bulkIgnorePatternFactory;

        /// <summary>
        /// Creates a new window depicting current failures/rules/outstanding for a given opened <paramref name="currentReport"/>
        /// </summary>
        /// <param name="currentReport"></param>
        /// <param name="ignorer">When the user uses this UI to ignore something what should happen</param>
        /// <param name="updater">When the user uses this UI to create a report rule what should happen</param>
        /// <param name="bulkIgnorePatternFactory">When the user bulk ignores many records at once how should the ignore patterns be generated</param>
        public RulesView(ReportReader currentReport, IgnoreRuleGenerator ignorer, RowUpdater updater, IRulePatternFactory bulkIgnorePatternFactory)
        {
            CurrentReport = currentReport;
            Ignorer = ignorer;
            Updater = updater;
            this.bulkIgnorePatternFactory = bulkIgnorePatternFactory;
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
                    case Key.DeleteChar:

                        var all = _treeView.GetAllSelectedObjects().ToArray();

                        var crn = _treeView.SelectedObject as CollidingRulesNode;
                        if(crn!=null && all.Length == 1)
                            Delete(crn);

                        var usages = all.OfType<RuleUsageNode>().ToArray();
                        if (usages.Any())
                        {
                            var answer = MessageBox.Query("Delete",$"Delete {usages.Length} Rules?", "Yes", "No");

                            if (answer == 0)
                            {
                                foreach (var u in usages)
                                    Delete(u);
                            }
                        }

                        e.Handled = true;

                        var ignoreAll = all.OfType<OutstandingFailureNode>().ToArray();
                        
                        if(ignoreAll.Any())
                        {
                            if(MessageBox.Query("Ignore",$"Ignore {ignoreAll.Length} failures?","Yes","No") == 0)
                            {
                                foreach (var f in ignoreAll)
                                {
                                    Ignore(f,ignoreAll.Length > 1);
                                }
                            }
                        }


                        break;
                    case Key.Enter:

                        var ofn = _treeView.SelectedObject as  OutstandingFailureNode;
                        
                        if(ofn != null)
                        {
                            Activate(ofn);
                        }

                        e.Handled = true;
                        return;
                }
            }
        }

        private void Delete(RuleUsageNode usage)
        {
            // tell ignorer to forget about this rule
            if(usage.Rulebase.Delete(usage.Rule))
                Remove(usage);
            else
                CouldNotDeleteRule();
            
        }

        private void Delete(CollidingRulesNode crn)
        {
            var answer = MessageBox.Query("Delete Rules","Which colliding rule do you want to delete?","Ignore","Update","Both","Cancel");

            if(answer == 0 || answer == 2)
            {
                // tell ignorer to forget about this rule
                if(Ignorer.Delete(crn.IgnoreRule))
                    Remove(crn);
                else
                    CouldNotDeleteRule();
            }
                
            if(answer == 1 || answer == 2)
            {
                // tell Updater to forget about this rule
                if(!Updater.Delete(crn.UpdateRule))
                    CouldNotDeleteRule();
                else
                    //no point removing it from UI twice
                    if(answer != 2)
                        Remove(crn);
            }
        }

        private void CouldNotDeleteRule()
        {
            MessageBox.ErrorQuery("Failed to Remove","Rule could not be found in rule base, perhaps yaml has non standard layout or embedded comments?","Ok");
        }

        private void Activate(OutstandingFailureNode ofn)
        {
            var ignore = new Button("Ignore");
            ignore.Clicked += ()=>{Ignore(ofn,false); Application.RequestStop();};

            var update = new Button("Update");
            update.Clicked += ()=>{Update(ofn); Application.RequestStop();};
            
            var cancel = new Button("Cancel");
            cancel.Clicked += ()=>{Application.RequestStop();};

            var dlg = new Dialog("Failure",MainWindow.DlgWidth,MainWindow.DlgHeight,ignore,update,cancel);

            var lbl = new FailureView(){
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(2),
                CurrentFailure = ofn.Failure
            };

            dlg.Add(lbl);

            Application.Run(dlg);
        }

        private void Update(OutstandingFailureNode ofn)
        {
            Updater.Add(ofn.Failure);
            Remove(ofn);
        }
        private void Ignore(OutstandingFailureNode ofn, bool isBulkIgnore)
        {
            if (isBulkIgnore)
            {
                Ignorer.Add(ofn.Failure, bulkIgnorePatternFactory);
            }
            else
            {
                Ignorer.Add(ofn.Failure);
            }
                
            Remove(ofn);
        }

        /// <summary>
        /// Removes the node from the tree
        /// </summary>
        /// <param name="obj"></param>
        private void Remove(ITreeNode obj)
        {
            var siblings = _treeView.GetParent(obj)?.Children;

            if(siblings == null)
            {
                return;
            }

            var idxToRemove = siblings.IndexOf(obj);
            
            if(idxToRemove == -1)
            {
                return;
            }


            // remove us
            siblings.Remove(obj);

            // but preserve the selected index
            if (idxToRemove < siblings.Count)
            {
                _treeView.SelectedObject = siblings[idxToRemove];
            }

            _treeView.RefreshObject(obj, true);
        }


        private void EvaluateRuleCoverage()
        {
            _treeView.ClearObjects();
            
            var colliding = new TreeNodeWithCount("Colliding Rules");
            var ignore = new TreeNodeWithCount("Ignore Rules Used");
            var update = new TreeNodeWithCount("Update Rules Used");
            var outstanding = new TreeNodeWithCount("Outstanding Failures");
                        
            var allRules = Ignorer.Rules.Union(Updater.Rules).ToList();

            AddDuplicatesToTree(allRules);

            _treeView.AddObjects(new []{ colliding,ignore,update,outstanding});

            var cts = new CancellationTokenSource();

            var btn = new Button("Cancel");
            Action cancelFunc = ()=>{cts.Cancel();};
            Action closeFunc = ()=>{Application.RequestStop();};
            btn.Clicked += cancelFunc;

            var dlg = new Dialog("Evaluating",MainWindow.DlgWidth,6,btn);

            var stage = new Label("Evaluating Failures"){Width = Dim.Fill(), X = 0,Y = 0};
            var progress = new ProgressBar(){Height= 2,Width = Dim.Fill(), X=0,Y = 1};
            var textProgress = new Label("0/0"){TextAlignment = TextAlignment.Right ,Width = Dim.Fill(), X=0,Y = 2};

            dlg.Add(stage);
            dlg.Add(progress);
            dlg.Add(textProgress);
                
            Task.Run(()=>{
                EvaluateRuleCoverageAsync(stage,progress,textProgress,cts.Token,colliding,ignore,update,outstanding);
                },cts.Token).ContinueWith((t)=>{
                
                    btn.Clicked -= cancelFunc;
                    btn.Text = "Done";
                    btn.Clicked += closeFunc;
                    dlg.SetNeedsDisplay();

                    cts.Dispose();
            });;
            
            Application.Run(dlg);
        }
        
        private void EvaluateRuleCoverageAsync(Label stage,ProgressBar progress, Label textProgress, CancellationToken token,TreeNodeWithCount colliding,TreeNodeWithCount ignore,TreeNodeWithCount update,TreeNodeWithCount outstanding)
        {
            Dictionary<IsIdentifiableRule,int> rulesUsed = new Dictionary<IsIdentifiableRule, int>();
            Dictionary<string,OutstandingFailureNode> outstandingFailures = new Dictionary<string, OutstandingFailureNode>();
            
            int done = 0;
            var max = CurrentReport.Failures.Count();

            foreach(Failure f in CurrentReport.Failures)
            {
                done++;
                token.ThrowIfCancellationRequested();
                if(done % 1000 == 0)
                    SetProgress(progress,textProgress,done,max);

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
                    // find an existing collision audit node for this input value
                    var existing = colliding.Children.OfType<CollidingRulesNode>().FirstOrDefault(c=>c.CollideOn[0].ProblemValue.Equals(f.ProblemValue));

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
            
            SetProgress(progress,textProgress,done,max);
            
            var ignoreRulesUsed = rulesUsed.Where(r=>r.Key.Action == RuleAction.Ignore).ToList();
            stage.Text = "Evaluating Ignore Rules Used";
            max = ignoreRulesUsed.Count();
            done = 0;

            foreach(var used in ignoreRulesUsed.OrderByDescending(kvp => kvp.Value))
            {
                done++;
                token.ThrowIfCancellationRequested();
                if(done % 1000 == 0)
                    SetProgress(progress,textProgress,done,max);

                ignore.Children.Add(new RuleUsageNode(Ignorer,used.Key,used.Value));
            }
            
            SetProgress(progress,textProgress,done,max);
                
            
            stage.Text = "Evaluating Update Rules Used";
            var updateRulesUsed = rulesUsed.Where(r=>r.Key.Action == RuleAction.Report).ToList();
            max = updateRulesUsed.Count();
            done = 0;

            foreach(var used in updateRulesUsed.OrderByDescending(kvp=>kvp.Value)){
                done++;

                token.ThrowIfCancellationRequested();
                if(done % 1000 == 0)
                    SetProgress(progress,textProgress,done,max);

                update.Children.Add(new RuleUsageNode(Updater,used.Key,used.Value)); 
            }
            
            SetProgress(progress,textProgress,done,max);

            stage.Text = "Evaluating Outstanding Failures";

            outstanding.Children = 
                outstandingFailures.Select(f=>f.Value).GroupBy(f=>f.Failure.ProblemField)               
                    .Select(g=>new FailureGroupingNode(g.Key,g.ToArray()))
                    .OrderByDescending(v=>v.Failures.Sum(f=>f.NumberOfTimesReported))
                    .Cast<ITreeNode>()
                    .ToList();
        }

        private void SetProgress(ProgressBar pb, Label tp, int done, int max)
        {
            if(max != 0)
                pb.Fraction = done/(float)max;
            tp.Text = $"{done:N0}/{max:N0}";
        }

        private void AddDuplicatesToTree(List<IsIdentifiableRule> allRules)
        {
            var root = new TreeNodeWithCount("Identical Rules");
            var children = GetDuplicates(allRules).ToArray();

            root.Children = children;
            _treeView.AddObject(root);
        }

        public IEnumerable<DuplicateRulesNode> GetDuplicates(IList<IsIdentifiableRule> rules)
        {
            // Find all rules that have identical patterns
            foreach(var dup in rules.Where(r=>!string.IsNullOrEmpty(r.IfPattern)).GroupBy(r=>r.IfPattern))
            {
                var duplicateRules = dup.ToArray();

                if(
                    // Multiple rules with same pattern
                    duplicateRules.Length > 1 &&
                    // targeting the same column
                    duplicateRules.Select(r=>r.IfColumn).Distinct().Count() == 1)
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
