using SmiServices.Common.Events;

namespace SmiServices.Common.Execution;

public interface IMicroserviceHost
{
    /// <summary>
    /// 
    /// </summary>
    event HostFatalHandler OnFatal;
}
