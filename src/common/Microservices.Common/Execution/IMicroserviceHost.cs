
using Microservices.Common.Events;

namespace Microservices.Common.Execution
{
    public interface IMicroserviceHost
    {
        /// <summary>
        /// 
        /// </summary>
        event HostFatalHandler OnFatal;
    }
}
