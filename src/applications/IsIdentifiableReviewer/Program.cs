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
                targets = d.Deserialize<List<Target>>(File.ReadAllText("Targets.yaml"));

                if (!targets.Any())
                    throw new Exception("No targets found in file");

            }
            catch (Exception e)
            {
                Console.WriteLine("Could not find/deserialize Targets.yaml");
                Console.WriteLine(e.ToString());
                returnCode = -1;
                return;
            }
            
            try
            {
                if(!string.IsNullOrWhiteSpace(opts.UnattendedOutputPath))
                {
                    //run unattended
                    if (targets.Count != 1)
                        throw new Exception("Unattended requires a single entry in Targets");

                    var unattended = new UnattendedReviewer(opts);
                    returnCode = unattended.Run();
                }
                else
                {
                    //run interactive
                    Application.Init();
                    var mainWindow = new MainWindow(targets,opts);
                    Application.Top.Add(mainWindow);
                    Application.Run();
                }
            }
            catch (Exception e)
            {
                Console.Write(e);
                returnCode = -1;
                return;
            }

            returnCode = 0;
        }
    }
}
