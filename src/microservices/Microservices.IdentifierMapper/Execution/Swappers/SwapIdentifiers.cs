using System.Diagnostics;
using NLog;
using Smi.Common.Options;

namespace Microservices.IdentifierMapper.Execution.Swappers
{
    public abstract class SwapIdentifiers: ISwapIdentifiers
    {
        public int CacheHit { get; protected set; }
        public int CacheMiss { get; protected set;}
        
        public int Success { get; protected set;}
        public int Fail { get; protected set;}
        public int Invalid { get; protected set;}

        public Stopwatch DatabaseStopwatch { get; } = new Stopwatch();
        
        public abstract void Setup(IMappingTableOptions mappingTableOptions);

        public abstract string GetSubstitutionFor(string toSwap, out string reason);

        public abstract void ClearCache();

        public virtual void LogProgress(ILogger logger, LogLevel level)
        {
            logger.Log(level,$"{GetType().Name}: CacheRatio={CacheHit}:{CacheMiss} SuccessRatio={Success}:{Fail}:{Invalid} DatabaseTime:{DatabaseStopwatch.Elapsed}");
        }
    }
}