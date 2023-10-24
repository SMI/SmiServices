
using System;
using JetBrains.Annotations;
using NLog;
using Smi.Common.Messaging;
using Smi.Common.Options;


namespace Microservices.DicomReprocessor
{
    public class DicomReprocessorControlMessageHandler : IControlMessageHandler
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        private readonly DicomReprocessorOptions _options;

        private const string Key = "set-sleep-time-ms";


        public DicomReprocessorControlMessageHandler(DicomReprocessorOptions options)
        {
            _options = options;
        }


        [UsedImplicitly]
        public void ControlMessageHandler(string action, string? message = null)
        {
            _logger.Info($"Received control event with action \"{action}\" and message \"{message}\"");

            if (!action.StartsWith(Key))
            {
                _logger.Info("Ignoring unknown action");
                return;
            }

            if (!int.TryParse(message, out int intTimeMs))
            {
                _logger.Error($"Couldn't parse a valid int from \"{message}\"");
                return;
            }

            TimeSpan newTime = TimeSpan.FromMilliseconds(intTimeMs);

            _logger.Info($"Setting batch sleep time to {newTime.TotalMilliseconds}ms");
            _options.SleepTime = newTime;
        }
    }
}
