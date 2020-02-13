using System;
using System.Linq;
using System.Text;
using LRUCache;
using NLog;
using Smi.Common.Options;
using StackExchange.Redis;

namespace Microservices.IdentifierMapper.Execution.Swappers
{
    /// <summary>
    /// A swapper that wraps <see cref="TableLookupWithGuidFallbackSwapper"/> and uses a Redis database (on localhost)
    /// to store cached values
    /// </summary>
    public class RedisSwapper : SwapIdentifiers,IDisposable
    {
        private readonly ConnectionMultiplexer _redis;
        private ISwapIdentifiers _hostedSwapper;

        private const string NullString = "NO MATCH";

        LRUCache<string,string> _memoryCache = new LRUCache<string, string>(1000);
        
        public RedisSwapper(string redisHost, ISwapIdentifiers wrappedSwapper)
        {
            _redis = ConnectionMultiplexer.Connect(redisHost);
            _hostedSwapper = wrappedSwapper;
        }
        public override void Setup(IMappingTableOptions mappingTableOptions)
        {
            _hostedSwapper.Setup(mappingTableOptions);
        }

        public override string GetSubstitutionFor(string toSwap, out string reason)
        {
            string output;
            reason = null;

            IDatabase db = _redis.GetDatabase();

            //lookup in memory
            var memCacheResult = _memoryCache.Get(toSwap);
            if (memCacheResult != null)
            {
                CacheHit++;
                Success++;
                return memCacheResult;
            }

            //look up Redis for a cached answer
            var val = db.StringGet(toSwap);

            //we have a cached answer (which might be null)
            if (val.HasValue)
            {
                output = val.ToString();
                CacheHit++;
            }
            else
            { 

                //we have no cached answer from Redis
                CacheMiss++;
            
                //Go to the hosted swapper
                output = _hostedSwapper.GetSubstitutionFor(toSwap, out reason);

                //and cache the result (even if it is null - no lookup match found)
                db.StringSet(toSwap, output ?? NullString, null, When.NotExists);
            }

            if (string.Equals(NullString, output))
            {
                output = null;
                reason = $"Value '{toSwap}' was cached in Redis as missing (i.e. no mapping was found)";
            }
                
            if (output == null)
                Fail++;
            else
            {
                //record in memory the value we matched
                _memoryCache.Add(toSwap,output);
                Success++;
            }

            return output;
        }


        public override void ClearCache()
        {
            _hostedSwapper.ClearCache();
        }

        public void Dispose()
        {
            _redis?.Dispose();
        }

        public override void LogProgress(ILogger logger, LogLevel level)
        {
            //output the Redis stats
            base.LogProgress(logger,level);

            //output the hosted mapper stats
            _hostedSwapper.LogProgress(logger,level);
        }
    }
}
