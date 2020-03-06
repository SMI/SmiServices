using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using FAnsi.Implementation;
using FAnsi.Implementations.MicrosoftSQL;
using FAnsi.Implementations.MySql;
using FAnsi.Implementations.Oracle;
using FAnsi.Implementations.PostgreSql;
using IsIdentifiableReviewer.Out;
using Terminal.Gui;
using YamlDotNet.Serialization;

namespace IsIdentifiableReviewer
{
    class Program
    {
        private static int returnCode;

        static int Main(string[] args)
        {
            ImplementationManager.Load<MySqlImplementation>();
            ImplementationManager.Load<OracleImplementation>();
            ImplementationManager.Load<MicrosoftSQLImplementation>();
            ImplementationManager.Load<PostgreSqlImplementation>();

            Parser.Default.ParseArguments<IsIdentifiableReviewerOptions>(args)
                .WithParsed(RunOptionsAndReturnExitCode)
                .WithNotParsed(errs => {   returnCode = 500;});
            
            return returnCode;
        }

        private static void RunOptionsAndReturnExitCode(IsIdentifiableReviewerOptions opts)
        {
            Deserializer d = new Deserializer();
            List<Target> targets;

            
            try
            {
                var file = new FileInfo(opts.TargetsFile);

                if (!file.Exists)
                {
                    Console.Write($"Could not find '{file.FullName}'");
                    returnCode = -1;
                    return;
                }

                var contents = File.ReadAllText(file.FullName);

                if (string.IsNullOrWhiteSpace(contents))
                {
                    Console.Write($"Targets file is empty '{file.FullName}'");
                    returnCode = -2;
                    return;
                }

                targets = d.Deserialize<List<Target>>(contents);

                if (!targets.Any())
                {
                    Console.Write($"Targets file did not contain any valid targets '{file.FullName}'");
                    returnCode = -3;
                    return;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error deserializing '{opts.TargetsFile}'");
                Console.WriteLine(e.Message);
                returnCode = -4;
                return;
            }

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
                returnCode = -10;
                return;

            }

                        
            var updater = new RowUpdater(new FileInfo(opts.RedList));
            var ignorer = new IgnoreRuleGenerator(new FileInfo(opts.IgnoreList));

            try
            {
                if(!string.IsNullOrWhiteSpace(opts.UnattendedOutputPath))
                {
                    //run unattended
                    if (targets.Count != 1)
                        throw new Exception("Unattended requires a single entry in Targets");

                    var unattended = new UnattendedReviewer(opts,targets.Single(),ignorer,updater);
                    returnCode = unattended.Run();
                }
                else
                {
                    Console.WriteLine("Press any key to launch GUI");
                    Console.ReadKey();

                    //run interactive
                    Application.Init();
                    var mainWindow = new MainWindow(targets,opts,ignorer,updater);
                    Application.Top.Add(mainWindow);
                    Application.Run();
                }
            }
            catch (Exception e)
            {
                Application.RequestStop();

                Console.Write(e);
                returnCode = -99;
                return;
            }

            returnCode = 0;
        }
    }
}
