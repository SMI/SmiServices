using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using IsIdentifiableReviewer.Out;
using Microservices.IsIdentifiable.Rules;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace IsIdentifiableReviewer
{
    class MainWindow : View
    {
        private readonly List<Target> _targets;

        public Target CurrentTarget { get; set; }
        public ReportReader CurrentReport { get; set; }

        public RuleGenerator Generator { get; set; } = new RuleGenerator(new FileInfo("NewRules.yaml"));

        public RowUpdater Updater { get; set; } = new RowUpdater();

        public int DlgWidth = 78;
        public int DlgHeight = 18;
        public int DlgBoundary = 2;
        private ValuePane _valuePane;
        private Label _info;
        private TextField _gotoTextField;

        public MainWindow(List<Target> targets)
        {
            _targets = targets;
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
                X=23,
                Width = 5
            };
            _gotoTextField.Changed += (s,e) => GoTo();
            frame.Add(_gotoTextField);

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

            top.Add (menu);
            Add(_info);
            Add(_valuePane);
            Add(frame);
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
                _valuePane.CurrentFailure = CurrentReport.Current;
            }
            catch (Exception e)
            {
                ShowException("Failed to GoTo",e);
            }
            
        }

        private void Next()
        {
            if(_valuePane.CurrentFailure == null)
                return;

            int skipped = 0;
            int updated = 0;

            while(CurrentReport.Next())
            {
                var next = CurrentReport.Current;

                if (!Generator.OnLoad(next))
                    skipped++;
                else if (!Updater.OnLoad(CurrentTarget,next))
                    updated++;
                else
                {
                    _valuePane.CurrentFailure = next;
                    break;
                }
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

            Generator.Add(_valuePane.CurrentFailure,RuleAction.Ignore);
            Next();
        }
        private void Update()
        {
            if(_valuePane.CurrentFailure == null)
                return;

            try
            {
                Updater.Update(CurrentTarget,_valuePane.CurrentFailure);
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
            if ( f != null)
            {
                try
                {
                    //if there are multiple targets
                    if (_targets.Count > 1)
                    {
                        //pick one
                        if (GetChoice("Target", "Pick the database this was generated from", out Target chosen,_targets.ToArray()))
                            CurrentTarget = chosen;
                        else
                            return; //they cancelled picking a target
                    }
                    else
                        CurrentTarget = _targets.Single();

                    CurrentReport = new ReportReader(new FileInfo(f));
                    _valuePane.CurrentFailure = CurrentReport.Failures.FirstOrDefault();
                    Next();
                }
                catch (Exception e)
                {
                    ShowException("Failed to Load",e);
                }

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
         
         public static string Wrap(string s, int width)
         {
             var r = new Regex(@"(?:((?>.{1," + width + @"}(?:(?<=[^\S\r\n])[^\S\r\n]?|(?=\r?\n)|$|[^\S\r\n]))|.{1,16})(?:\r?\n)?|(?:\r?\n|$))");
             return r.Replace(s, "$1\n");
         }
         
    }
}
