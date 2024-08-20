using NLog;
using SmiServices.Common.Options;
using System;
using System.IO;
using System.IO.Abstractions;


namespace SmiServices.Common
{
    public static class SmiLogging
    {
        private const string DefaultLogConfigName = "Smi.NLog.config";

        private static bool _initialised;

        public static void Setup(LoggingOptions loggingOptions, string hostProcessName, IFileSystem? fileSystem = null)
        {
            if (_initialised)
                throw new Exception("SmiLogging already initialised");
            _initialised = true;

            fileSystem ??= new FileSystem();

            string localConfig = fileSystem.Path.Combine(fileSystem.Directory.GetCurrentDirectory(), DefaultLogConfigName);
            string configFilePathToLoad = !string.IsNullOrWhiteSpace(loggingOptions.LogConfigFile)
                ? loggingOptions.LogConfigFile
                : localConfig;

            if (!fileSystem.File.Exists(configFilePathToLoad))
                throw new FileNotFoundException($"Could not find the specified logging configuration '{configFilePathToLoad})'");

            LogManager.ThrowConfigExceptions = true;
            LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration(configFilePathToLoad);

            if (!string.IsNullOrWhiteSpace(loggingOptions.LogsRoot))
            {
                if (!fileSystem.Directory.Exists(loggingOptions.LogsRoot))
                    throw new ApplicationException($"Invalid log root '{loggingOptions.LogsRoot}'");

                VerifyCanWrite(loggingOptions.LogsRoot, fileSystem);

                LogManager.Configuration.Variables["baseFileName"] =
                    $"{loggingOptions.LogsRoot}/" +
                    $"{hostProcessName}/" +
                    $"${{cached:cached=true:clearCache=None:inner=${{date:format=yyyy-MM-dd-HH-mm-ss}}}}-${{processid}}";
            }
            else
                VerifyCanWrite(fileSystem.Directory.GetCurrentDirectory(), fileSystem);

            Logger logger = LogManager.GetLogger(nameof(SmiLogging));
            LogManager.GlobalThreshold = LogLevel.Trace;

            if (!loggingOptions.TraceLogging)
                LogManager.GlobalThreshold = LogLevel.Debug;
            logger.Trace("Trace logging enabled!");

            logger.Info($"Logging config loaded from {configFilePathToLoad}");
        }

        private static void VerifyCanWrite(string logsRoot, IFileSystem fileSystem)
        {
            string[] tmpFileParts = fileSystem.Path.GetTempFileName().Split(fileSystem.Path.DirectorySeparatorChar);
            string tmpFileInLogsRoot = fileSystem.Path.Combine(logsRoot, tmpFileParts[^1]);
            try
            {
                fileSystem.File.WriteAllText(tmpFileInLogsRoot, "");
            }
            catch (UnauthorizedAccessException e)
            {
                throw new UnauthorizedAccessException($"Couldn't create a file in the logs root '{logsRoot}'; possible permissions error", e);
            }
            finally
            {
                fileSystem.File.Delete(tmpFileInLogsRoot);
            }
        }
    }
}
