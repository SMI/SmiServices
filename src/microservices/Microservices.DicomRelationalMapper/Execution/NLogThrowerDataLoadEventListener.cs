using System;
using NLog;
using ReusableLibraryCode.Progress;

namespace Microservices.DicomRelationalMapper.Execution
{
    internal class NLogThrowerDataLoadEventListener:IDataLoadEventListener
    {
        private readonly Logger _logger;
        private ThrowImmediatelyDataLoadEventListener _thrower = new(){WriteToConsole = false};

        public NLogThrowerDataLoadEventListener(Logger logger)
        {
            _logger = logger;
        }

        public void OnNotify(object sender, NotifyEventArgs e)
        {
            _logger.Log(ToLogLevel(e.ProgressEventType), e.Exception, e.Message);
            _thrower.OnNotify(sender,e);
        }

        private static LogLevel ToLogLevel(ProgressEventType type) =>
            type switch
            {
                ProgressEventType.Trace => LogLevel.Trace,
                ProgressEventType.Debug => LogLevel.Debug,
                ProgressEventType.Information => LogLevel.Info,
                ProgressEventType.Warning => LogLevel.Warn,
                ProgressEventType.Error => LogLevel.Error,
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };

        public void OnProgress(object sender, ProgressEventArgs e)
        {
            _thrower.OnProgress(sender,e);
        }
    }
}
