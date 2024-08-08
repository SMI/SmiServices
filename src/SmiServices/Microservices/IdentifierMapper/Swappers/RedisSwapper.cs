using FAnsi.Discovery;
using Microsoft.Extensions.Caching.Memory;
using NLog;
using SmiServices.Common.Options;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace SmiServices.Microservices.IdentifierMapper.Swappers
{
    /// <summary>
    /// A swapper that wraps <see cref="TableLookupWithGuidFallbackSwapper"/> and uses a Redis database (on localhost)
    /// to store cached values
    /// </summary>
    public class RedisSwapper : SwapIdentifiers, IDisposable
    {
        private readonly ConnectionMultiplexer _redis;
        private readonly ISwapIdentifiers _hostedSwapper;

        private const string NullString = "NO MATCH";

        private readonly MemoryCache _cache = new(new MemoryCacheOptions
        {
            SizeLimit = 1024
        });

        private readonly ConcurrentDictionary<object, SemaphoreSlim> _locks = new();

        private readonly ILogger _logger;

        public RedisSwapper(string redisHost, ISwapIdentifiers wrappedSwapper)
        {
            _logger = LogManager.GetCurrentClassLogger();
            _redis = ConnectionMultiplexer.Connect(redisHost);
            _hostedSwapper = wrappedSwapper;
        }

        public override void Setup(IMappingTableOptions mappingTableOptions)
        {
            _hostedSwapper.Setup(mappingTableOptions);
        }

        public override string? GetSubstitutionFor(string toSwap, out string? reason)
        {
            reason = null;

            //lookup in memory
            if (!_cache.TryGetValue(toSwap, out string? result))
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
                            Interlocked.Increment(ref CacheHit);
                        }
                        else
                        {
                            //we have no cached answer from Redis
                            Interlocked.Increment(ref CacheMiss);

                            //Go to the hosted swapper
                            lock (_hostedSwapper)
                            {
                                result = _hostedSwapper.GetSubstitutionFor(toSwap, out reason);
                            }

                            //and cache the result (even if it is null - no lookup match found)
                            db.StringSet(toSwap, result ?? NullString);
                        }

                        _cache.Set(toSwap, result ?? NullString, new MemoryCacheEntryOptions
                        {
                            Size = 1
                        });
                    }
                }
                finally
                {
                    locket.Release();
                }
            }
            else
            {
                Interlocked.Increment(ref CacheHit);
            }

            if (string.Equals(NullString, result))
            {
                result = null;
                reason = $"Value '{toSwap}' was cached in Redis as missing (i.e. no mapping was found)";
            }

            if (result == null)
                Interlocked.Increment(ref Fail);
            else
            {
                int res = Interlocked.Increment(ref Success);
                if (res % 1000 == 0)
                    LogProgress(_logger, LogLevel.Info);
            }

            return result;
        }


        public override void ClearCache()
        {
            _hostedSwapper.ClearCache();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            _redis?.Dispose();
        }

        public override void LogProgress(ILogger logger, LogLevel level)
        {
            //output the Redis stats
            base.LogProgress(logger, level);

            //output the hosted mapper stats
            _hostedSwapper.LogProgress(logger, level);
        }

        public override DiscoveredTable? GetGuidTableIfAny(IMappingTableOptions options)
        {
            return _hostedSwapper.GetGuidTableIfAny(options);
        }
    }
}
