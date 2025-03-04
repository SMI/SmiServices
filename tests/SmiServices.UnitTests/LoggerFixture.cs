using NLog;
using NLog.Config;
using NLog.Targets;

namespace SmiServices.UnitTests;

public static class LoggerFixture
{
    private const string TestLoggerName = "TestLogger";

    public static void Setup()
    {
        if (LogManager.Configuration == null)
            LogManager.Configuration = new LoggingConfiguration();
        else if (LogManager.Configuration.FindTargetByName<ConsoleTarget>(TestLoggerName) != null)
            return;

        var consoleTarget = new ConsoleTarget(TestLoggerName)
        {
            Layout = @"${longdate}|${level:padding=-5}|${logger}|${message}|${exception:format=toString,Data:maxInnerExceptionLevel=5}",
            AutoFlush = true
        };

        LoggingConfiguration config = LogManager.Configuration;
        config.AddTarget(consoleTarget);
        config.AddRuleForAllLevels(consoleTarget);

        LogManager.GlobalThreshold = LogLevel.Trace;
        LogManager.GetCurrentClassLogger().Info("TestLogger added to LogManager config");
    }
}
