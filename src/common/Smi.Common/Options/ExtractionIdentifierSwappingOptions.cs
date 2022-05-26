using FAnsi;
using FAnsi.Discovery;
using System;

namespace Smi.Common.Options
{
    public class ExtractionIdentifierSwappingOptions : IMappingTableOptions
    {
        public string MappingConnectionString { get; set; }

        public string MappingTableName { get; set; }
        public string SwapColumnName { get; set; }
        public string ReplacementColumnName { get; set; }

        public DatabaseType MappingDatabaseType { get; set; }

        public int TimeoutInSeconds { get; set; }
        public string SwapperType { get; set; }

        /// <summary>
        /// Optional, if set then your <see cref="SwapperType"/> will be wrapped and it's answers cached in this Redis database.
        /// The Redis database will always be consulted for a known answer first and <see cref="SwapperType"/> used
        /// as a fallback.
        /// See https://stackexchange.github.io/StackExchange.Redis/Configuration.html#basic-configuration-strings for the format.
        /// </summary>
        public string RedisConnectionString { get; set; }

        public IMappingTableOptions Clone()
        {
            return new ExtractionIdentifierSwappingOptions
            {
                MappingConnectionString = MappingConnectionString,
                MappingTableName = MappingTableName,
                MappingDatabaseType = MappingDatabaseType,
                TimeoutInSeconds = TimeoutInSeconds,
                ReplacementColumnName = ReplacementColumnName,
                SwapColumnName = SwapColumnName,
                SwapperType = SwapperType,
                RedisConnectionString = RedisConnectionString
            };
        }

        public DiscoveredTable Discover()
        {
            return IdentifierMapperOptions.Discover(this);
        }

        public bool IsEmpty()
        {
            return string.IsNullOrEmpty(MappingConnectionString);
        }
    }
}