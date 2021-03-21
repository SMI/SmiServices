using IsIdentifiableReviewer;
using IsIdentifiableReviewer.Out;
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

            Deserializer d = new Deserializer();
            List<Target> targets;

            try
            {
                var file = new FileInfo(opts.TargetsFile);

                if (!file.Exists)
                {
                    Console.Write($"Could not find '{file.FullName}'");
                    return 1;
                }

                var contents = File.ReadAllText(file.FullName);

                if (string.IsNullOrWhiteSpace(contents))
                {
                    Console.Write($"Targets file is empty '{file.FullName}'");
                    return 2;
                }

                targets = d.Deserialize<List<Target>>(contents);

                if (!targets.Any())
                {
                    Console.Write($"Targets file did not contain any valid targets '{file.FullName}'");
                    return 3;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error deserializing '{opts.TargetsFile}'");
                Console.WriteLine(e.Message);
                return 4;
            }

            if (opts.OnlyRules)
                Console.WriteLine("Skipping Connection Tests");
            else
            {
                Console.WriteLine("Running Connection Tests");

                try
                {
                    foreach (Target t in targets)
                        Console.WriteLine(t.Discover().Exists()
                            ? $"Successfully connected to {t.Name}"
                            : $"Failed to connect to {t.Name}");
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error Validating Targets");
                    Console.WriteLine(e.ToString());
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

                    //run interactive
                    Application.Init();

                    var top = Application.Top;

                    var mainWindow = new MainWindow(opts, ignorer, updater);
                    

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
                Console.Write(e.Message);

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
