using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using IsIdentifiableReviewer.Out;
using Microservices.IsIdentifiable.Reporting;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace IsIdentifiableReviewer
{
    class MainWindow : View,IRulePatternFactory
    {
        private readonly List<Target> _targets;

        public Target CurrentTarget { get; set; }
        public ReportReader CurrentReport { get; set; }

        public IgnoreRuleGenerator Ignorer { get; }

        public RowUpdater Updater { get;  } 

        public int DlgWidth = 78;
        public int DlgHeight = 18;
        public int DlgBoundary = 2;
        private ValuePane _valuePane;
        private Label _info;
        private TextField _gotoTextField;
        private IRulePatternFactory _origUpdaterRulesFactory;
        private IRulePatternFactory _origIgnorerRulesFactory;
        private Label _ignoreRuleLabel;
        private Label _updateRuleLabel;
        private CheckBox _cbRulesOnly;

        Stack<MainWindowHistory> History = new Stack<MainWindowHistory>();

        public MainWindow(List<Target> targets, IsIdentifiableReviewerOptions opts, IgnoreRuleGenerator ignorer, RowUpdater updater)
        {
            _targets = targets;
            Ignorer = ignorer;
            Updater = updater;
            _origUpdaterRulesFactory = updater.RulesFactory;
            _origIgnorerRulesFactory = ignorer.RulesFactory;

            X = 0;
            Y = 1;
            Width = Dim.Fill();
            Height = Dim.Fill();
            
            var top = Application.Top;

            var menu = new MenuBar (new MenuBarItem [] {
                new MenuBarItem ("_File (F9)", new MenuItem [] {
                    new MenuItem("_Open Report",null, OpenReport), 
                    new MenuItem ("_Quit", null, () => { top.Running = false; })
                })
            });

            _info = new Label("Info")
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = 1
            };
            
            _info.TextColor = Attribute.Make(Color.Black,Color.Gray);
            
            _valuePane = new ValuePane()
            {
                X = 0,
                Y = 1,
                Width = Dim.Fill(),
                Height = 10,
            };

            var frame = new FrameView("Options")
            {
                X = 0,
                Y = 11,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };

            frame.Add(new Button("Ignore")
            {
                X = 0,
                Clicked = Ignore
            });

            frame.Add(new Button("Update")
            {
                X = 11,
                Clicked = Update
            });
            
            _gotoTextField = new TextField("1")
            {
                X=28,
                Width = 5
            };
            _gotoTextField.Changed += (s,e) => GoTo();
            frame.Add(_gotoTextField);
            frame.Add(new Label(23,0,"GoTo:"));

            frame.Add(new Button("Prev")
            {
                X = 0,
                Y = 1,
                Clicked = ()=>GoToRelative(-1)
            });
            frame.Add(new Button("Next")
            {
                X = 11,
                Y = 1,
                Clicked = ()=>GoToRelative(1)
            });

            frame.Add(new Button("unDo")
            {
                X = 11,
                Y = 2,
                Clicked = ()=>Undo()
            });

            frame.Add(new Label(0,4,"Default Patterns"));

            _ignoreRuleLabel = new Label(0,5,"Ignore:");
            _updateRuleLabel= new Label(0,6,"Update:");;
            frame.Add(_ignoreRuleLabel);
            frame.Add(_updateRuleLabel);

            var cbCustomPattern = new CheckBox(23,1,"Custom Patterns",false);
            cbCustomPattern.Toggled += (c, s) =>
            {
                Updater.RulesFactory = cbCustomPattern.Checked ? this : _origUpdaterRulesFactory;
                Ignorer.RulesFactory = cbCustomPattern.Checked ? this : _origIgnorerRulesFactory;
            };
            frame.Add(cbCustomPattern);

            _cbRulesOnly = new CheckBox(23,2,"Rules Only",opts.OnlyRules);
            Updater.RulesOnly = opts.OnlyRules;

            _cbRulesOnly.Toggled += (c, s) => { Updater.RulesOnly = _cbRulesOnly.Checked;};
            frame.Add(_cbRulesOnly);
            
            top.Add (menu);
            Add(_info);
            Add(_valuePane);
            Add(frame);

            if(!string.IsNullOrWhiteSpace(opts.FailuresCsv))
                OpenReport(opts.FailuresCsv,(e)=>throw e, (t)=>throw new Exception("Mode only supported when a single Target is configured"));
        }

        private void Undo()
        {
            if (History.Count == 0)
            {
                ShowMessage("History Empty","Cannot undo, history is empty");
                return;
            }

            var popped = History.Pop();

            //undo file history
            popped.OutputBase.Undo();

            //wind back UI
            GoTo(popped.Index);
        }

        private void GoToRelative(int offset)
        {
            if(CurrentReport == null)
                return;

            GoTo(CurrentReport.CurrentIndex + offset);
        }

        private void GoTo()
        {
            if(CurrentReport == null)
                return;

            try
            {
                GoTo(int.Parse(_gotoTextField.Text.ToString()));
            }
            catch (FormatException)
            {
                //use typed in 'hello there! or some such'
            }
        }
        
        private void GoTo(int page)
        {
            if(CurrentReport == null)
                return;
            try
            {
                CurrentReport.GoTo(page);
                _info.Text = CurrentReport.DescribeProgress();
                SetupToShow(CurrentReport.Current);
            }
            catch (Exception e)
            {
                ShowException("Failed to GoTo",e);
            }
            
        }

        private void SetupToShow(Failure f)
        {
            _valuePane.CurrentFailure = f;

            if (f != null)
            {
                _ignoreRuleLabel.Text = "Ignore:" + _origIgnorerRulesFactory.GetPattern(Ignorer, f);
                _updateRuleLabel.Text = "Update:" + _origUpdaterRulesFactory.GetPattern(Ignorer, f);
            }
            else
            {
                _ignoreRuleLabel.Text = "Ignore:";
                _updateRuleLabel.Text = "Update:";
            }
        }

        private void Next()
        {
            if(_valuePane.CurrentFailure == null)
                return;

            int skipped = 0;
            int updated = 0;
            try
            {
                while(CurrentReport.Next())
                {
                    var next = CurrentReport.Current;

                    //prefer rules that say we should update the database with redacted over rules that say we should ignore the problem
                    if (!Updater.OnLoad(CurrentTarget?.Discover(),next, out _))
                        updated++;
                    else if (!Ignorer.OnLoad(next,out _))
                        skipped++;
                    else
                    {
                        SetupToShow(next);

                        break;
                    }
                }
            }
            catch (Exception e)
            {
                ShowException("Error moving to next record",e);
            }
            
            if(CurrentReport.Exhausted)
                ShowMessage("End", "End of Failures");
            
            StringBuilder info = new StringBuilder();

            info.Append(CurrentReport.DescribeProgress());

            if (skipped > 0)
                info.Append(" Skipped " + skipped);
            if (updated > 0)
                info.Append(" Auto Updated " + updated);

            _info.Text = info.ToString();
        }
        
        private void Ignore()
        {
            if(_valuePane.CurrentFailure == null)
                return;

            try
            {
                Ignorer.Add(_valuePane.CurrentFailure);
                History.Push(new MainWindowHistory(CurrentReport.CurrentIndex,Ignorer));
            }
            catch (OperationCanceledException)
            {
                //if user cancels adding the ignore then stay on the same record
                return;
            }
            Next();
        }
        private void Update()
        {
            if(_valuePane.CurrentFailure == null)
                return;

            try
            {
                Updater.Update(_cbRulesOnly.Checked ? null : CurrentTarget?.Discover()
                    , _valuePane.CurrentFailure, null /*create one yourself*/);

                History.Push(new MainWindowHistory(CurrentReport.CurrentIndex,Updater));
            }
            catch (OperationCanceledException)
            {
                //if user cancels updating then stay on the same record
                return;
            }
            catch (Exception e)
            {
                ShowException("Failed to update database",e);
                return;
            }

            Next();
        }

        private void OpenReport()
        {
            var ofd = new OpenDialog("Load CSV Report", "Enter file path to load")
            {
                AllowedFileTypes = new[] {".csv"}, 
                CanChooseDirectories = false,
                AllowsMultipleSelection = false
            };

            Application.Run(ofd);

            var f = ofd.FilePaths?.SingleOrDefault();

            OpenReport(f,
                (e)=>ShowException("Failed to Load", e),
                (t)=> 
                    GetChoice("Target", "Pick the database this was generated from", out Target chosen,t.ToArray())
                    ? chosen : null);
        }

        private void OpenReport(string path, Action<Exception> exceptionHandler, Func<IEnumerable<Target>,Target> targetPicker)
        {
            if ( path == null)
                return;

            try
            {
                //if there are multiple targets
                CurrentTarget = _targets.Count > 1 ? targetPicker(_targets) : _targets.Single();

                if(CurrentTarget == null)
                    return;

                CurrentReport = new ReportReader(new FileInfo(path));
                SetupToShow(CurrentReport.Failures.FirstOrDefault());
                Next();
            }
            catch (Exception e)
            {
                exceptionHandler(e);
            }
        }

        public void ShowMessage(string title, string body)
        {
            RunDialog(title,body,out _,"Ok");
        }

        private void ShowException(string msg, Exception e)
        {
            var e2 = e;
            const string stackTraceOption = "Stack Trace";
            StringBuilder sb = new StringBuilder();

            while (e2 != null)
            {
                sb.AppendLine(e2.Message);
                e2 = e2.InnerException;
            }

            if(GetChoice(msg, sb.ToString(), out string chosen, "Ok", stackTraceOption))
                if(string.Equals(chosen,stackTraceOption))
                    ShowMessage("Stack Trace",e.ToString());

        }
        public bool GetChoice<T>(string title, string body, out T chosen, params T[] options)
        {
            return RunDialog(title, body, out chosen, options);
        }

         bool RunDialog<T>(string title, string message,out T chosen, params T[] options)
        {
            var result = default(T);
            bool optionChosen = false;

            var dlg = new Dialog(title, Math.Min(Console.WindowWidth,DlgWidth), DlgHeight);
            
            var line = DlgHeight - (DlgBoundary)*2 - options.Length;

            if (!string.IsNullOrWhiteSpace(message))
            {
                int width = Math.Min(Console.WindowWidth,DlgWidth) - (DlgBoundary * 2);

                var msg = Wrap(message, width-1).TrimEnd();

                var text = new Label(0, 0, msg)
                {
                    Height = line - 1, Width = width
                };

                //if it is too long a message
                int newlines = msg.Count(c => c == '\n');
                if (newlines > line - 1)
                {
                    var view = new ScrollView(new Rect(0, 0, width, line - 1))
                    {
                        ContentSize = new Size(width, newlines + 1),
                        ContentOffset = new Point(0, 0),
                        ShowVerticalScrollIndicator = true,
                        ShowHorizontalScrollIndicator = false
                    };
                    view.Add(text);
                    dlg.Add(view);
                }
                else
                    dlg.Add(text);
            }
            
            foreach (var value in options)
            {
                T v1 = (T) value;

                string name = value.ToString();

                var btn = new Button(0, line++, name)
                {
                    Clicked = () =>
                    {
                        result = v1;
                        dlg.Running = false;
                        optionChosen = true;
                    }
                };


                dlg.Add(btn);

                if(options.Length == 1)
                    dlg.FocusFirst();
            }

            Application.Run(dlg);

            chosen = result;
            return optionChosen;
        }
         private bool GetText(string title, string message, string initialValue, out string chosen,
             Dictionary<string, string> buttons)
         {
            bool optionChosen = false;

            var dlg = new Dialog(title, Math.Min(Console.WindowWidth,DlgWidth), DlgHeight);

            var line = DlgHeight - (DlgBoundary)*2 - 2;

            if (!string.IsNullOrWhiteSpace(message))
            {
                int width = Math.Min(Console.WindowWidth,DlgWidth) - (DlgBoundary * 2);

                var msg = Wrap(message, width-1).TrimEnd();

                var text = new Label(0, 0, msg)
                {
                    Height = line - 1, Width = width
                };

                //if it is too long a message
                int newlines = msg.Count(c => c == '\n');
                if (newlines > line - 1)
                {
                    var view = new ScrollView(new Rect(0, 0, width, line - 1))
                    {
                        ContentSize = new Size(width, newlines + 1),
                        ContentOffset = new Point(0, 0),
                        ShowVerticalScrollIndicator = true,
                        ShowHorizontalScrollIndicator = false
                    };
                    view.Add(text);
                    dlg.Add(view);
                }
                else
                    dlg.Add(text);
            }

            var txt = new TextField(0, line++, DlgWidth -4 ,initialValue ?? "");
            dlg.Add(txt);

            var btn = new Button(0, line, "Ok")
            {
                IsDefault = true,
                Clicked = () =>
                {
                    if (!string.IsNullOrWhiteSpace(txt.Text?.ToString()))
                    {
                        dlg.Running = false;
                        optionChosen = true;
                    }
                }
            };
            dlg.Add(btn);

            int x = 10;
            if(buttons != null)
                foreach (var kvp in buttons)
                {
                    var button = new Button(x, line,kvp.Key)
                    {
                        Clicked = () => { txt.Text = kvp.Value; }
                    };
                    dlg.Add(button);
                    x += kvp.Key.Length + 5;
                }

            dlg.FocusFirst();
        

            Application.Run(dlg);

            chosen = txt.Text?.ToString();
            return optionChosen;
         }
         
         public static string Wrap(string s, int width)
         {
             var r = new Regex(@"(?:((?>.{1," + width + @"}(?:(?<=[^\S\r\n])[^\S\r\n]?|(?=\r?\n)|$|[^\S\r\n]))|.{1,16})(?:\r?\n)?|(?:\r?\n|$))");
             return r.Replace(s, "$1\n");
         }

         public string GetPattern(object sender,Failure failure)
         {
             var defaultFactory = sender == Updater ? _origUpdaterRulesFactory : _origIgnorerRulesFactory;

             var recommendedPattern = defaultFactory.GetPattern(sender,failure);
            
             Dictionary<string,string> buttons = new Dictionary<string, string>();
             buttons.Add("Clear","");
             buttons.Add("Full",_origIgnorerRulesFactory.GetPattern(sender,failure));
             buttons.Add("Captures",_origUpdaterRulesFactory.GetPattern(sender,failure));
             
             buttons.Add("Symbols",new SymbolsRulesFactory().GetPattern(sender,failure));

             if (GetText("Pattern", "Enter pattern to match failure", recommendedPattern, out string chosen,buttons))
             {
                 Regex regex;

                 try
                 {
                     regex = new Regex(chosen);
                 }
                 catch (Exception)
                 {
                    ShowMessage("Invalid Regex","Pattern was not a valid Regex");
                    //try again!
                    return GetPattern(sender,failure);
                 }

                 if (!regex.IsMatch(failure.ProblemValue))
                 {
                     GetChoice("Pattern Match Failure","The provided pattern did not match the original ProblemValue.  Try a different pattern?",out string retry,new []{"Yes","No"});

                     if (retry == "Yes")
                         return GetPattern(sender,failure);
                 }
                 
                 if(string.IsNullOrWhiteSpace(chosen))
                     throw new Exception("User entered blank Regex pattern");

                 return chosen;
             }
                
            
             throw new OperationCanceledException("User chose not to enter a pattern");
         }

    }
}
