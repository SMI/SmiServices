using System.Diagnostics;
using FAnsi.Discovery;
using NLog;
using Smi.Common.Options;

namespace Microservices.IdentifierMapper.Execution.Swappers
{
    public abstract class SwapIdentifiers: ISwapIdentifiers
    {
        public int CacheHit;
        public int CacheMiss;

        public int Success;
        public int Fail;
        public int Invalid { get; protected set;}

        public Stopwatch DatabaseStopwatch { get; } = new Stopwatch();

        public abstract void Setup(IMappingTableOptions mappingTableOptions);

        public abstract string? GetSubstitutionFor(string toSwap, out string? reason);

        public abstract void ClearCache();

        public virtual void LogProgress(ILogger logger, LogLevel level)
        {
            logger.Log(level,$"{GetType().Name}: CacheRatio={CacheHit}:{CacheMiss} SuccessRatio={Success}:{Fail}:{Invalid} DatabaseTime:{DatabaseStopwatch.Elapsed}");
        }

        public abstract DiscoveredTable GetGuidTableIfAny(IMappingTableOptions options);
    }
}
