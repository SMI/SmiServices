using System;
using NLog;
using ReusableLibraryCode.Progress;

namespace Microservices.DicomRelationalMapper.Execution
{
    internal class NLogThrowerDataLoadEventListener:IDataLoadEventListener
    {
        private Logger _logger;
        private ThrowImmediatelyDataLoadEventListener _thrower = new();

        public NLogThrowerDataLoadEventListener(Logger logger)
        {
            _logger = logger;
        }

        public void OnNotify(object sender, NotifyEventArgs e)
        {
            _logger.Log(ToLogLevel(e.ProgressEventType), e.Exception, e.Message);
            _thrower.OnNotify(sender,e);
        }

        private LogLevel ToLogLevel(ProgressEventType t)
        {
            switch (t)
            {
                case ProgressEventType.Trace:
                    return LogLevel.Trace;
                case ProgressEventType.Debug:
                    return LogLevel.Debug;
                case ProgressEventType.Information:
                    return LogLevel.Info;
                case ProgressEventType.Warning:
                    return LogLevel.Warn;
                case ProgressEventType.Error:
                    return LogLevel.Error;
                default:
                    throw new ArgumentOutOfRangeException("t");
            }
        }

        public void OnProgress(object sender, ProgressEventArgs e)
        {
            _thrower.OnProgress(sender,e);
        }
    }
}
