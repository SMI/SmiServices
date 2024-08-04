using CommandLine;
using NLog;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Rdmp.Core.ReusableLibraryCode;
using Rdmp.Core.ReusableLibraryCode.Checks;
using Rdmp.Core.ReusableLibraryCode.Progress;
using Terminal.Gui;
using Setup;
using SmiServices.Common;

namespace SmiServices.Applications.Setup;
public class Program
{
    class SetupOptions
    {
        [Option("usc", Required = false, HelpText = "Use the NetDriver console driver.  This can fix some compatibility issues with some environments.")]
        public bool UseSystemConsole { get; set; }

        [Option('y', Required = false, HelpText = "Unattended Yaml validation mode.  Provide path to yaml settings file.  No gui is launched. Summary only is produced and exit code is nonzero for any failing service")]
        public string? UnattendedYaml { get; set; }
    }

    public static int Main(IEnumerable<string> args)
    {
        int exitCode = 0;

        FansiImplementations.Load();

        Parser.Default.ParseArguments<SetupOptions>(args)
                   .WithParsed(o =>
                   {
                       if (string.IsNullOrWhiteSpace(o.UnattendedYaml))
                       {
                           RunGui(o);
                       }
                       else
                       {
                           exitCode = RunUnattended(o);
                       }
                   });
        return exitCode;
    }

    private static int RunUnattended(SetupOptions o)
    {
        var probe = new EnvironmentProbe(o.UnattendedYaml);

        if (probe.DeserializeYaml.Result == CheckResult.Fail)
        {
            ReportFailure(probe.DeserializeYaml);
            return 10;
        }

        var listenInfra = new ToMemoryDataLoadEventListener(false);
        var listenMicro = new ToMemoryDataLoadEventListener(false);

        var checkInfraTask = Task.Run(() => probe.CheckInfrastructure(listenInfra));
        var checkMicroTask = Task.Run(() => probe.CheckMicroservices(listenMicro));

        // Synchronous
        AnsiConsole.Progress()
            .Start(ctx =>
            {
                // Define tasks
                var consoleTaskCheckInfra = ctx.AddTask("[green]Checking Infrastructure[/]");
                var consoleTaskCheckMicro = ctx.AddTask("[green]Checking Microservices [/]");

                while (!ctx.IsFinished)
                {
                    SetProgress(consoleTaskCheckInfra, listenInfra, EnvironmentProbe.CheckInfrastructureTaskName);
                    SetProgress(consoleTaskCheckMicro, listenMicro, EnvironmentProbe.CheckMicroservicesTaskName);
                }
            });

        // Create a table
        var table = new Table();

        // Add some columns
        table.AddColumn("Name");
        table.AddColumn("Result");
        table.AddColumn("Exception");

        AddResult(table, probe.DeserializeYaml, nameof(probe.DeserializeYaml));

        foreach (var p in probe.Probes)
        {
            AddResult(table, p.Value.Result, p.Value.Name);
        }

        // Render the table to the console
        AnsiConsole.Write(table);


        return probe.GetExitCode();
    }

    private static void AddResult(Table table, CheckEventArgs? result, string taskName)
    {
        StringBuilder sb = new StringBuilder();

        if (result != null)
        {
            if (result.Ex != null)
            {
                sb.AppendLine(result.Message);
                sb.AppendLine(ExceptionHelper.ExceptionToListOfInnerMessages(result.Ex));
            }
            else
            {
                sb.AppendLine(result.Message);
            }
        }
        table.AddRow(taskName,
            result == null ? "Not Attempted" : result.Result == CheckResult.Fail ? "[red]Fail[/]" : "[green]Success[/]",
            result?.Result == CheckResult.Fail ? sb.ToString().Trim() : ""
            );
    }

    private static void ReportFailure(CheckEventArgs e)
    {
        if (e.Ex != null)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine(ExceptionHelper.ExceptionToListOfInnerMessages(e.Ex));
        }
        else
        {
            Console.WriteLine(e.Message);
        }
    }

    private static void SetProgress(ProgressTask consoleTask, ToMemoryDataLoadEventListener listener, string taskName)
    {
        if (listener.LastProgressRecieivedByTaskName.ContainsKey(taskName))
        {
            var progress = listener.LastProgressRecieivedByTaskName[taskName];

            consoleTask.Value = progress.Progress.Value;
            consoleTask.MaxValue = progress.Progress.KnownTargetValue;
        }
        else
        {
            // job has not started yet
            consoleTask.Value = 0;
            consoleTask.MaxValue = 1;
        }
    }

    private static void RunGui(SetupOptions o)
    {
        Application.UseSystemConsole = o.UseSystemConsole;

        Application.Init();

        Application.Driver.UnChecked = 'x';

        // don't log to console!
        LogManager.SuspendLogging();

        Application.Run(new MainWindow(), (e) =>
        {
            MessageBox.ErrorQuery("Global Error", e.ToString(), "Ok");
            return true;
        });

        Application.Shutdown();
    }
}
