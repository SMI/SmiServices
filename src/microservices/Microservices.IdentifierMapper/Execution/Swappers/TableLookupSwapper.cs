
using Smi.Common.Options;
using NLog;
using FAnsi.Discovery;
using System;
using System.Data.Common;

namespace Microservices.IdentifierMapper.Execution.Swappers
{
    /// <summary>
    /// Connects to a database containing values to swap identifiers with. Keeps a single cache of the last seen value
    /// </summary>
    public class TableLookupSwapper : ISwapIdentifiers
    {
        public int TotalSwapCount { get; private set; }
        public int TotalCachedSwapCount { get; private set; }


        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        private DiscoveredServer _server;
        private IMappingTableOptions _options;
        private DiscoveredTable _swapTable;

        // Simple cache of the last swap pair
        private string _lastKey;
        private string _lastVal;
        

        public void Setup(IMappingTableOptions options)
        {
            _options = options;
            _swapTable =  options.Discover();
            _server = _swapTable.Database.Server;

            if(!_swapTable.Exists())
                throw new ArgumentException($"Swap table '{_swapTable.GetFullyQualifiedName()}' did not exist on server '{_server}'");
        }

        public string GetSubstitutionFor(string toSwap, out string reason)
        {
            reason = null;

            // If the cached key matches, return the last value
            if (string.Equals(toSwap, _lastKey) && _lastVal != null)
            {
                _logger.Debug("Using cached swap value");

                ++TotalCachedSwapCount;
                ++TotalSwapCount;

                return _lastVal;
            }

            // Else fall through to the database lookup
            using (DbConnection con = _server.GetConnection())
            {
                con.Open();

                string sql = string.Format("SELECT {0} FROM {1} WHERE {2}=@val",
                    _options.ReplacementColumnName,
                    _swapTable.GetFullyQualifiedName(),
                    _options.SwapColumnName);

                DbCommand cmd = _server.GetCommand(sql, con);
                _server.AddParameterWithValueToCommand("@val", cmd, toSwap);

                object result = cmd.ExecuteScalar();

                if (result == DBNull.Value || result == null)
                {
                    reason = "No match found for '" + toSwap + "'";
                    return null;
                }

                _lastKey = toSwap;
                _lastVal = result.ToString();

                ++TotalSwapCount;

                return _lastVal;
            }
        }

        public void ClearCache()
        {
            _lastVal = null;
            _logger.Debug("ClearCache called, single value cache cleared");
        }
    }
}
