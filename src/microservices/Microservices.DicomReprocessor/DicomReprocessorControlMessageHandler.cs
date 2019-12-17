
using Microservices.DicomReprocessor.Options;
using NLog;


namespace Microservices.DicomReprocessor
{
    public class DicomReprocessorControlMessageHandler
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        private readonly DicomReprocessorCliOptions _options;

        private const string Key = "set-sleep-time";


        public DicomReprocessorControlMessageHandler(DicomReprocessorCliOptions options)
        {
            _options = options;
        }


        public void ControlMessageHandler(string action, string message = null)
        {
            //NOTE (RKM 2019-12-16) Expecting action to be "set-sleep-time-<int>". Can we use message instead?

            _logger.Info("Received control event with action " + action);

            if (!action.StartsWith(Key))
                return;

            // Debug
            _logger.Info($"SUBSTRING: {action.Substring(Key.Length)}");

            if (!int.TryParse(action.Substring(Key.Length), out int newTime) || newTime < 0)
            {
                _logger.Error($"Couldn't parse a valid time from \"action\"");
                return;
            }

            _logger.Info($"Setting batch sleep time to {newTime}");
            _options.SleepTime = newTime;
        }
    }
}
