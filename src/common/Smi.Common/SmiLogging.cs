using System;
using System.IO;
using NLog;


namespace Smi.Common
{
    public class SmiLogging
    {
        public void Setup()
        {
            /*
            logConfigPath = !string.IsNullOrWhiteSpace(globals.FileSystemOptions.LogConfigFile)
                ? globals.FileSystemOptions.LogConfigFile
                : Path.Combine(globals.CurrentDirectory, "Smi.NLog.config");

            if (!File.Exists(logConfigPath))
                throw new FileNotFoundException("Could not find the logging configuration in the current directory (Smi.NLog.config), or at the path specified by FileSystemOptions.LogConfigFile");

            LogManager.ThrowConfigExceptions = true;
            LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration(logConfigPath);

            // add a test to make sure destination is writeable and throws a warning
            if (globals.FileSystemOptions.ForceSmiLogsRoot)
            {
                string smiLogsRoot = globals.LogsRoot;

                if (string.IsNullOrWhiteSpace(smiLogsRoot) || !Directory.Exists(smiLogsRoot))
                    throw new ApplicationException($"Invalid logs root: {smiLogsRoot}");

                LogManager.Configuration.Variables["baseFileName"] =
                    $"{smiLogsRoot}/{HostProcessName}/${{cached:cached=true:clearCache=None:inner=${{date:format=yyyy-MM-dd-HH-mm-ss}}}}-${{processid}}";
            }


            if (!string.IsNullOrWhiteSpace(logConfigPath))
                Logger.Debug($"Logging config loaded from {logConfigPath}");

            if (!globals.MicroserviceOptions.TraceLogging)
                LogManager.GlobalThreshold = LogLevel.Debug;

            Logger = LogManager.GetLogger(GetType().Name);
            Logger.Trace("Trace logging enabled!");
            */
        }
    }
}
