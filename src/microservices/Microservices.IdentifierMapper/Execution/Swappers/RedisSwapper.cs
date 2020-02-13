using System;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Extensions.Caching.Memory;
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

        private MemoryCache _cache = new MemoryCache(new MemoryCacheOptions()
        {
            SizeLimit = 1024
        });
        private ConcurrentDictionary<object, SemaphoreSlim> _locks = new ConcurrentDictionary<object, SemaphoreSlim>();

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
            string result;
            reason = null;

            //lookup in memory
            if (!_cache.TryGetValue(toSwap, out result))
            {
                SemaphoreSlim locket = _locks.GetOrAdd(toSwap, k => new SemaphoreSlim(1, 1));
                locket.Wait();
                try
                {
                    if (!_cache.TryGetValue(toSwap, out result))
                    {
                        // Now try Redis cache
                        IDatabase db = _redis.GetDatabase();
                        var val = db.StringGet(toSwap);
                        //we have a cached answer (which might be null)
                        if (val.HasValue)
                        {
                            result = val.ToString();
                            CacheHit++;
                        }
                        else
                        {
                            //we have no cached answer from Redis
                            CacheMiss++;

                            //Go to the hosted swapper
                            lock(_hostedSwapper)
                            {
                                result = _hostedSwapper.GetSubstitutionFor(toSwap, out reason);
                            }

                            //and cache the result (even if it is null - no lookup match found)
                            db.StringSet(toSwap, result ?? NullString, null, When.NotExists);
                        }

                        _cache.Set(toSwap, result);
                    }
                }
                finally
                {
                    locket.Release();
                }
            }
            else
            {
                CacheHit++;
                Success++;
            }
            return result;
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
