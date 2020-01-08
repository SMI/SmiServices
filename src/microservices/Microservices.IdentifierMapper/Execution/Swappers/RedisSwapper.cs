using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private TableLookupWithGuidFallbackSwapper _hostedSwapper;

        public const string RedistServer = "localhost";

        public RedisSwapper()
        {
            _redis = ConnectionMultiplexer.Connect(RedistServer);
            _hostedSwapper = new TableLookupWithGuidFallbackSwapper();
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
            using(var admin = ConnectionMultiplexer.Connect(RedistServer +",allowAdmin=true"))
                foreach (var server in admin.GetEndPoints().Select(e=> admin.GetServer(e)))
                    server.FlushAllDatabases();
            
            _hostedSwapper.ClearCache();
        }

        public void Dispose()
        {
            _redis?.Dispose();
        }
    }
}
