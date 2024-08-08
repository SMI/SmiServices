using FAnsi.Discovery;
using NLog;
using SmiServices.Common;
using SmiServices.Common.Options;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace SmiServices.Microservices.IdentifierMapper.Swappers
{
    /// <summary>
    /// Connects to a database containing values to swap identifiers with, and loads it entirely into memory
    /// </summary>
    public class PreloadTableSwapper : SwapIdentifiers
    {
        private readonly ILogger _logger;

        private IMappingTableOptions? _options;

        private Dictionary<string, string>? _mapping;
        private readonly object _oDictionaryLock = new();


        public PreloadTableSwapper()
        {
            _logger = LogManager.GetCurrentClassLogger();
        }

        /// <summary>
        /// Preloads the swap table into memory
        /// </summary>
        /// <param name="options"></param>
        [MemberNotNull(nameof(_mapping))]
        public override void Setup(IMappingTableOptions options)
        {
            _logger.Info("Setting up mapping dictionary");

            using (new TimeTracker(DatabaseStopwatch))
                lock (_oDictionaryLock)
                {
                    _options = options;

                    DiscoveredTable tbl = options.Discover();

                    using DbConnection con = tbl.Database.Server.GetConnection();
                    con.Open();

                    string sql =
                        $"SELECT {options.SwapColumnName}, {options.ReplacementColumnName} FROM {tbl.GetFullyQualifiedName()}";
                    _logger.Debug($"SQL: {sql}");

                    DbCommand cmd = tbl.Database.Server.GetCommand(sql, con);
                    cmd.CommandTimeout = _options.TimeoutInSeconds;

                    DbDataReader dataReader = cmd.ExecuteReader();

                    _mapping = [];

                    _logger.Debug("Populating dictionary from mapping table...");
                    Stopwatch sw = Stopwatch.StartNew();

                    while (dataReader.Read())
                        _mapping.Add(dataReader[_options.SwapColumnName!].ToString()!, dataReader[_options.ReplacementColumnName!].ToString()!);

                    _logger.Debug("Mapping dictionary populated with " + _mapping.Count + " entries in " + sw.Elapsed.ToString("g"));
                }
        }

        public override string? GetSubstitutionFor(string toSwap, out string? reason)
        {
            lock (_oDictionaryLock)
            {
                if (!_mapping!.ContainsKey(toSwap))
                {
                    reason = "PatientID was not in mapping table";
                    Fail++;
                    CacheMiss++;
                    return null;
                }

                reason = null;
            }

            Success++;
            CacheHit++;
            return _mapping[toSwap];
        }

        /// <summary>
        /// Clears the cached table and reloads it from the database
        /// </summary>
        public override void ClearCache()
        {
            _logger.Debug("Clearing cache and reloading");

            if (_options == null)
                throw new ApplicationException("ClearCache called before mapping options set");

            Setup(_options);
        }

        public override DiscoveredTable? GetGuidTableIfAny(IMappingTableOptions options)
        {
            return null;
        }
    }
}
