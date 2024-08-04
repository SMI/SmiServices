using System;
using NLog;
using Rdmp.Core.ReusableLibraryCode.Progress;

namespace SmiServices.Microservices.DicomRelationalMapper
{
    internal sealed class NLogThrowerDataLoadEventListener : IDataLoadEventListener
    {
        private readonly Logger _logger;
        private static readonly ThrowImmediatelyDataLoadEventListener _thrower = ThrowImmediatelyDataLoadEventListener.Quiet;

        public NLogThrowerDataLoadEventListener(Logger logger)
        {
            _logger = logger;
        }

        public void OnNotify(object sender, NotifyEventArgs e)
        {
            _logger.Log(ToLogLevel(e.ProgressEventType), e.Exception, e.Message);
            _thrower.OnNotify(sender, e);
        }

        private static LogLevel ToLogLevel(ProgressEventType t) =>
            t switch
            {
                ProgressEventType.Trace => LogLevel.Trace,
                ProgressEventType.Debug => LogLevel.Debug,
                ProgressEventType.Information => LogLevel.Info,
                ProgressEventType.Warning => LogLevel.Warn,
                ProgressEventType.Error => LogLevel.Error,
                _ => throw new ArgumentOutOfRangeException(nameof(t))
            };

        public void OnProgress(object sender, ProgressEventArgs e)
        {
            _thrower.OnProgress(sender, e);
        }
    }
}
