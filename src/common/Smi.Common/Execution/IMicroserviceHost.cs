
using Smi.Common.Events;

namespace Smi.Common.Execution
{
    public interface IMicroserviceHost
    {
        /// <summary>
        /// 
        /// </summary>
        event HostFatalHandler OnFatal;
    }
}
