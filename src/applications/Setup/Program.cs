using NLog;
using Setup;
using Smi.Common;
using System.Linq;
using Terminal.Gui;

class Program
{    public static void Main(string[] args)
    {
        Application.UseSystemConsole = args.Any(a => string.Equals(a, "--usc") || string.Equals(a, "-usc"));

        Application.Init();

        Application.Driver.UnChecked = 'x';
        
        FansiImplementations.Load();

        // don't log to console!
        LogManager.SuspendLogging();

        Application.Run(new MainWindow(), (e) => {
            MessageBox.ErrorQuery("Global Error", e.ToString(), "Ok");
            return true;
        });

        Application.Shutdown();
    }
}

