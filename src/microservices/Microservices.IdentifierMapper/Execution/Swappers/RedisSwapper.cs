using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            IDatabase db = _redis.GetDatabase();
            
            var val = db.StringGet(toSwap);

            if (val.HasValue)
            {
                reason = null;
                CacheHit++;
                Success++;
                return val.ToString();
            }

            CacheMiss++;
            
            var output = _hostedSwapper.GetSubstitutionFor(toSwap, out reason);
            db.StringSet(toSwap, output, null, When.NotExists);

            Success++;
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
