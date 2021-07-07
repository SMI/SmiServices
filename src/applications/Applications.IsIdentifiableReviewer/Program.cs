using IsIdentifiableReviewer;
using IsIdentifiableReviewer.Out;
using NLog;
using Smi.Common;
using Smi.Common.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terminal.Gui;
using YamlDotNet.Serialization;


namespace Applications.IsIdentifiableReviewer
{
    public static class Program
    {
        public static int Main(IEnumerable<string> args)
        {
            int ret = SmiCliInit
                .ParseAndRun<IsIdentifiableReviewerOptions>(
                    args,
                    typeof(Program),
                    OnParse
                );
            return ret;
        }

        private static int OnParse(GlobalOptions globals, IsIdentifiableReviewerOptions opts)
        {
            FansiImplementations.Load();
            var logger = LogManager.GetCurrentClassLogger();

            opts.FillMissingWithValuesUsing(globals.IsIdentifiableReviewerOptions);

            Deserializer d = new Deserializer();
            List<Target> targets;

            try
            {
                var file = new FileInfo(opts.TargetsFile);

                if (!file.Exists)
                {
                    logger.Error($"Could not find '{file.FullName}'");
                    return 1;
                }

                var contents = File.ReadAllText(file.FullName);

                if (string.IsNullOrWhiteSpace(contents))
                {
                    logger.Error($"Targets file is empty '{file.FullName}'");
                    return 2;
                }

                targets = d.Deserialize<List<Target>>(contents);

                if (!targets.Any())
                {
                    logger.Error($"Targets file did not contain any valid targets '{file.FullName}'");
                    return 3;
                }
            }
            catch (Exception e)
            {
                logger.Error(e,$"Error deserializing '{opts.TargetsFile}'");
                return 4;
            }

            if (opts.OnlyRules)
                logger.Info("Skipping Connection Tests");
            else
            {
                logger.Info("Running Connection Tests");

                try
                {
                    foreach (Target t in targets)
                        Console.WriteLine(t.Discover().Exists()
                            ? $"Successfully connected to {t.Name}"
                            : $"Failed to connect to {t.Name}");
                }
                catch (Exception e)
                {
                    logger.Error(e,"Error Validating Targets");
                    return 10;
                }
            }

            //for updater try to match the ProblemValue words
            var updater = new RowUpdater(new FileInfo(opts.RedList))
            {
                RulesOnly = opts.OnlyRules,
                RulesFactory = new MatchProblemValuesPatternFactory()
            };

            //for Ignorer match the whole string
            var ignorer = new IgnoreRuleGenerator(new FileInfo(opts.IgnoreList));

            try
            {
                if (!string.IsNullOrWhiteSpace(opts.UnattendedOutputPath))
                {
                    //run unattended
                    if (targets.Count != 1)
                        throw new Exception("Unattended requires a single entry in Targets");

                    var unattended = new UnattendedReviewer(opts, targets.Single(), ignorer, updater);
                    return unattended.Run();
                }
                else
                {
                    Console.WriteLine("Press any key to launch GUI");
                    Console.ReadKey();


                    if (opts.UseSystemConsole)
                    {
                        Application.UseSystemConsole = true;
                    }
                        

                    //run interactive
                    Application.Init();

                    if (opts.Theme != null && opts.Theme.Exists)
                    {
                        try
                        {
                            var des = new Deserializer();
                            var theme = des.Deserialize<TerminalGuiTheme>(File.ReadAllText(opts.Theme.FullName));

                            Colors.Base = theme.Base.GetScheme();
                            Colors.Dialog = theme.Dialog.GetScheme();
                            Colors.Error = theme.Error.GetScheme();
                            Colors.Menu = theme.Menu.GetScheme();
                            Colors.TopLevel = theme.TopLevel.GetScheme();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.ErrorQuery("Could not deserialize theme",ex.Message);
                        }
                    }

                    var top = Application.Top;

                    var mainWindow = new MainWindow(globals, opts, ignorer, updater);
                    

                    // Creates the top-level window to show
                    var win = new Window("IsIdentifiable Reviewer")
                    {
                        X = 0,
                        Y = 1, // Leave one row for the toplevel menu

                        // By using Dim.Fill(), it will automatically resize without manual intervention
                        Width = Dim.Fill(),
                        Height = Dim.Fill()
                    };

                    top.Add(win);

                    top.Add(mainWindow.Menu);

                    win.Add(mainWindow.Body);

                    Application.Run();

                    return 0;
                }
            }
            catch (Exception e)
            {
                logger.Error(e, $"Application crashed");

                int tries = 5;
                while (Application.Top != null && tries-- > 0)
                    try
                    {
                        Application.RequestStop();
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Failed to terminate GUI on crash");
                    }

                return 99;
            }
        }
    }
}
