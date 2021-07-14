using IsIdentifiableReviewer.Out;
using Microservices.IsIdentifiable.Failures;
using Microservices.IsIdentifiable.Reporting;
using Microservices.IsIdentifiable.Rules;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Terminal.Gui;
using Terminal.Gui.Trees;

namespace IsIdentifiableReviewer.Views
{
    class RulesView : View
    {
        public ReportReader CurrentReport { get; private set; }
        public IgnoreRuleGenerator Ignorer { get; private set; }
        public RowUpdater Updater { get; private set; }

        private TreeView _treeView;

        /// <summary>
        /// When the user bulk ignores many records at once how should the ignore patterns be generated
        /// </summary>
        private IRulePatternFactory bulkIgnorePatternFactory;


        Label lblInitialSummary;

        public RulesView()
        {
            Width = Dim.Fill();
            Height = Dim.Fill();

            lblInitialSummary = new Label("No report loaded") { Width = Dim.Fill() };
            Add(lblInitialSummary);

            var lblEvaluate = new Label($"Evaluate:") { Y = Pos.Bottom(lblInitialSummary) + 1 };
            Add(lblEvaluate);


            var ruleCollisions = new Button("Rule Coverage")
            {
                Y = Pos.Bottom(lblEvaluate)
            };

            ruleCollisions.Clicked += () => EvaluateRuleCoverage();
            Add(ruleCollisions);

            _treeView = new TreeView()
            {
                Y = Pos.Bottom(ruleCollisions) + 1,
                Width = Dim.Fill(),
                Height = Dim.Fill(1)
            };
            _treeView.KeyPress += _treeView_KeyPress;
            _treeView.ObjectActivated += _treeView_ObjectActivated;
            _treeView.SelectionChanged += _treeView_SelectionChanged;

            Add(_treeView);
        }


        private void _treeView_SelectionChanged(object sender, SelectionChangedEventArgs<ITreeNode> e)
        {
            if(e.NewValue != null)
            {
                e.Tree.RefreshObject(e.NewValue);
            }
            
            // when selecting a node 

            if (e.NewValue is OutstandingFailureNode ofn){

                // if it is now covered by an existing rule! Like maybe they have 500 outstanding failures 
                // and on the first one they add a rule .* (ignore EVERYTHING) then we had better disappear the rest of the tree too

                var ignoreRule = Ignorer.Rules.FirstOrDefault(r => r.Apply(ofn.Failure.ProblemField, ofn.Failure.ProblemValue, out _) != RuleAction.None);

                if (ignoreRule != null)
                {
                    Remove(ofn);
                    return;
                }

                var updateRule = Updater.Rules.FirstOrDefault(r => r.Apply(ofn.Failure.ProblemField, ofn.Failure.ProblemValue, out _) != RuleAction.None);

                if(updateRule != null)
                {
                    Remove(ofn);
                    return;
                }
            }
        }

        /// <summary>
        /// Sets up the UI to show a new report
        /// </summary>
        /// <param name="currentReport"></param>
        /// <param name="ignorer">When the user uses this UI to ignore something what should happen</param>
        /// <param name="updater">When the user uses this UI to create a report rule what should happen</param>
        /// <param name="bulkIgnorePatternFactory">When the user bulk ignores many records at once how should the ignore patterns be generated</param>
        public void LoadReport(ReportReader currentReport, IgnoreRuleGenerator ignorer, RowUpdater updater, IRulePatternFactory bulkIgnorePatternFactory)
        {
            CurrentReport = currentReport;
            Ignorer = ignorer;
            Updater = updater;
            this.bulkIgnorePatternFactory = bulkIgnorePatternFactory;

            lblInitialSummary.Text = $"There are {ignorer.Rules.Count} ignore rules and {updater.Rules.Count} update rules.  Current report contains {CurrentReport.Failures.Length:N0} Failures";
            
        }

        private void _treeView_ObjectActivated(ObjectActivatedEventArgs<ITreeNode> obj)
        {
            var ofn = obj.ActivatedObject as OutstandingFailureNode;

            if (ofn != null)
            {
                Activate(ofn);
            }
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
            ignore.Clicked += ()=> {
                try
                {
                    Ignore(ofn, false);
                }
                catch (OperationCanceledException)
                {
                    // user cancelled the interactive ignore e.g. with Ctrl+Q
                }

                Application.RequestStop();
            };

            var update = new Button("Update");
            update.Clicked += ()=>{
                try
                {
                    Update(ofn);
                }
                catch (OperationCanceledException)
                {
                    // user cancelled the interactive update e.g. with Ctrl+Q
                }

                Application.RequestStop();
            };
            
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
                try
                {
                    Ignorer.Add(ofn.Failure);
                }
                catch (OperationCanceledException)
                {
                    // user cancelled the interactive dialog
                    return;
                }
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

            if(Ignorer == null || Updater == null || CurrentReport == null)
            {
                return;
            }
                
            
            var colliding = new TreeNodeWithCount("Colliding Rules");
            var ignore = new TreeNodeWithCount("Ignore Rules Used");
            var update = new TreeNodeWithCount("Update Rules Used");
            var outstanding = new TreeNodeWithCount("Outstanding Failures");
                        
            var allRules = Ignorer.Rules.Union(Updater.Rules).ToList();

            AddDuplicatesToTree(allRules);


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


            bool done = false;

            Application.MainLoop.AddTimeout(TimeSpan.FromSeconds(1), (s) =>
            {
                dlg.SetNeedsDisplay();
                return !done;
            });

            Task.Run(()=>{
                EvaluateRuleCoverageAsync(stage,progress,textProgress,cts.Token,colliding,ignore,update,outstanding);
                },cts.Token).ContinueWith((t,s)=>{
                
                    btn.Clicked -= cancelFunc;
                    btn.Text = "Done";
                    btn.Clicked += closeFunc;
                    done = true;
                    cts.Dispose();

                    _treeView.RebuildTree();
                    _treeView.AddObjects(new[] { colliding, ignore, update, outstanding });
                },SynchronizationContext.Current);
            
            Application.Run(dlg);
        }
        
        private void EvaluateRuleCoverageAsync(Label stage,ProgressBar progress, Label textProgress, CancellationToken token,TreeNodeWithCount colliding,TreeNodeWithCount ignore,TreeNodeWithCount update,TreeNodeWithCount outstanding)
        {
            ConcurrentDictionary<IsIdentifiableRule,int> rulesUsed = new ConcurrentDictionary<IsIdentifiableRule, int>();
            ConcurrentDictionary<string,OutstandingFailureNode> outstandingFailures = new ConcurrentDictionary<string, OutstandingFailureNode>();
            
            int done = 0;
            var max = CurrentReport.Failures.Count();
            object lockObj = new object();


            var result = Parallel.ForEach(CurrentReport.Failures,
                (f) =>
                {
                    token.ThrowIfCancellationRequested();

                    if (Interlocked.Increment(ref done) % 10000 == 0)
                        SetProgress(progress, textProgress, done, max);

                    var ignoreRule = Ignorer.Rules.FirstOrDefault(r => r.Apply(f.ProblemField, f.ProblemValue, out _) != RuleAction.None);
                    var updateRule = Updater.Rules.FirstOrDefault(r => r.Apply(f.ProblemField, f.ProblemValue, out _) != RuleAction.None);

                    // record how often each reviewer rule was used with a failure
                    foreach (var r in new[] { ignoreRule, updateRule })
                        if (r != null)
                        {
                            lock(lockObj)
                            {
                                rulesUsed.AddOrUpdate(r, 1, (k, v) => Interlocked.Increment(ref v));
                            }
                        }

                    // There are 2 conflicting rules for this input value (it should be updated and ignored!)
                    if (ignoreRule != null && updateRule != null)
                    {
                        lock (lockObj)
                        {
                            // find an existing collision audit node for this input value
                            var existing = colliding.Children.OfType<CollidingRulesNode>().FirstOrDefault(c => c.CollideOn[0].ProblemValue.Equals(f.ProblemValue));

                            if (existing != null)
                                existing.Add(f);
                            else
                                colliding.Children.Add(new CollidingRulesNode(ignoreRule, updateRule, f));
                        }
                    }

                    // input value that doesn't match any system rules yet
                    if (ignoreRule == null && updateRule == null)
                    {
                        lock (lockObj)
                        {
                            outstandingFailures.AddOrUpdate(f.ProblemValue, new OutstandingFailureNode(f, 1), (k, v) => {
                                Interlocked.Increment(ref v.NumberOfTimesReported);
                                return v;
                            });
                        }
                    }
                });

            if (!result.IsCompleted)
                throw new OperationCanceledException();

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
    }
}
