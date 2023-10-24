
using Microservices.IdentifierMapper.Execution.Swappers;
using NLog;
using Smi.Common.Messaging;

namespace Microservices.IdentifierMapper.Messaging
{
    public class IdentifierMapperControlMessageHandler : IControlMessageHandler
    {
        private readonly ILogger _logger;

        private readonly ISwapIdentifiers _swapper;


        public IdentifierMapperControlMessageHandler(ISwapIdentifiers swapper)
        {
            _swapper = swapper;
            _logger = LogManager.GetCurrentClassLogger();
        }

        public void ControlMessageHandler(string action, string? message = null)
        {
            _logger.Info("Received control event with action " + action);

            // Only 1 event to handle - cache refresh

            if (action != "refresh")
                return;

            _logger.Info("Refreshing cached swapper dictionary");

            _swapper.ClearCache();
        }
    }
}
