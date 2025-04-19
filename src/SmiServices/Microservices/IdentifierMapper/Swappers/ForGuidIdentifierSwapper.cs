using FAnsi.Discovery;
using NLog;
using SmiServices.Common;
using SmiServices.Common.Options;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using TypeGuesser;

namespace SmiServices.Microservices.IdentifierMapper.Swappers;

/// <summary>
/// Connects to a (possibly empty) database containing values to swap identifiers with. If no valid replacement found for a value,
/// we create a new <see cref="Guid"/>, insert it into the database, and return it as the swapped value. Keeps a cache of swap values
/// </summary>
public class ForGuidIdentifierSwapper : SwapIdentifiers
{
    private readonly ILogger _logger;

    private IMappingTableOptions? _options;

    private DiscoveredTable? _table;

    private readonly Dictionary<string, string> _cachedAnswers = [];
    private readonly object _oCacheLock = new();

    private int _swapColumnLength;

    public ForGuidIdentifierSwapper()
    {
        _logger = LogManager.GetCurrentClassLogger();
    }

    /// <summary>
    /// Connects to the specified swapping table if it exists, or creates it
    /// </summary>
    /// <param name="mappingTableOptions"></param>
    public override void Setup(IMappingTableOptions mappingTableOptions)
    {
        // TODO(rkm 2021-04-09) Check if this can be in a constructor instead?
        _options = mappingTableOptions;
        _table = _options.Discover();

        using (new TimeTracker(DatabaseStopwatch))
            CreateTableIfNotExists();
    }

    public override string? GetSubstitutionFor(string toSwap, out string? reason)
    {
        reason = null;

        if (_swapColumnLength > 0 && toSwap.Length > _swapColumnLength)
        {
            reason = $"Supplied value was too long ({toSwap.Length}) - max allowed is ({_swapColumnLength})";
            Invalid++;
            return null;
        }

        string insertSql;
        lock (_oCacheLock)
        {
            if (_cachedAnswers.TryGetValue(toSwap, out string? value))
            {
                CacheHit++;
                Success++;
                return value;
            }


            var guid = Guid.NewGuid().ToString();

            insertSql = _options!.MappingDatabaseType switch
            {
                FAnsi.DatabaseType.MicrosoftSQLServer => string.Format(
                    "if not exists( select 1 from {0} where {1} = '{3}') insert into {0}({1},{2}) values ('{3}','{4}')",
                    _table!.GetRuntimeName(), _options.SwapColumnName, _options.ReplacementColumnName, toSwap,
                    guid),
                FAnsi.DatabaseType.MySql =>
                    $"INSERT IGNORE INTO {_table!.GetFullyQualifiedName()} SET {_options.SwapColumnName} = '{toSwap}', {_options.ReplacementColumnName} = '{guid}';",
                FAnsi.DatabaseType.PostgreSql =>
                    $"INSERT INTO {_table!.GetFullyQualifiedName()} ({_table.GetQuerySyntaxHelper().EnsureWrapped(_options.SwapColumnName)},{_table.GetQuerySyntaxHelper().EnsureWrapped(_options.ReplacementColumnName)}) VALUES ('{toSwap}','{guid}') ON CONFLICT DO NOTHING;",
                _ => throw new ArgumentOutOfRangeException(_options.MappingConnectionString)
            };

            using (new TimeTracker(DatabaseStopwatch))
            using (var con = _table.Database.Server.BeginNewTransactedConnection())
            {
                DbCommand cmd = _table.Database.Server.GetCommand(insertSql, con);

                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    Invalid++;
                    throw new Exception("Failed to perform lookup of toSwap with SQL:" + insertSql, e);
                }

                //guid may not have been inserted.  Just because we don't have it in our cache doesn't mean that other people might
                //not have allocated that one at the same time.

                DbCommand cmd2 = _table.Database.Server.GetCommand($"SELECT {_table.GetQuerySyntaxHelper().EnsureWrapped(_options.ReplacementColumnName)} FROM {_table.GetFullyQualifiedName()} WHERE {_table.GetQuerySyntaxHelper().EnsureWrapped(_options.SwapColumnName)} = '{toSwap}'  ", con);
                var syncAnswer = (string?)cmd2.ExecuteScalar() ?? throw new Exception("Replacement value was null");

                _cachedAnswers.Add(toSwap, syncAnswer);

                con.ManagedTransaction?.CommitAndCloseConnection();
                Success++;
                CacheMiss++;
                return syncAnswer;
            }
        }
    }

    /// <summary>
    /// Clears the in-memory cache of swap pairs
    /// </summary>
    public override void ClearCache()
    {
        lock (_oCacheLock)
        {
            _cachedAnswers.Clear();
            _logger.Info("Cache cleared");
        }
    }


    private void CreateTableIfNotExists()
    {
        try
        {
            ArgumentNullException.ThrowIfNull(_table, nameof(_table));
            ArgumentNullException.ThrowIfNull(_options, nameof(_options));
            ArgumentNullException.ThrowIfNull(_options.SwapColumnName, nameof(_options.SwapColumnName));
            ArgumentNullException.ThrowIfNull(_options.ReplacementColumnName, nameof(_options.ReplacementColumnName));

            //create the database if it doesn't exist
            if (!_table.Database.Exists())
                _table.Database.Create();

            //create the table if it doesn't exist
            if (!_table.Exists())
            {
                _logger.Info("Guid mapping table does not exist, creating it now");

                _table.Database.CreateTable(
                    _table.GetRuntimeName(),
                    [
                        new DatabaseColumnRequest(_options.SwapColumnName, new DatabaseTypeRequest(typeof(string), 10), false){ IsPrimaryKey = true },
                        new DatabaseColumnRequest(_options.ReplacementColumnName,new DatabaseTypeRequest(typeof(string), 255), false),
                    ],
                    schema: _table.Schema
                );
            }

            if (_table.Exists())
                _logger.Info("Guid mapping table exist (" + _table + ")");
            else
                throw new Exception("Table creation did not result in table existing!");

            _logger.Info("Checking for column " + _options!.SwapColumnName);
            _swapColumnLength = _table.DiscoverColumn(_options.SwapColumnName).DataType?.GetLengthIfString() ?? -1;

            _logger.Info("Checking for column " + _options.ReplacementColumnName);
            _table.DiscoverColumn(_options.ReplacementColumnName);
        }
        catch (Exception e)
        {
            var sb = new StringBuilder();

            if (_table != null)
            {
                sb.AppendLine("Server:" + _table.Database.Server.Name);
                sb.AppendLine("Database:" + _table.Database.GetRuntimeName());
                sb.AppendLine("Username:" + _table.Database.Server.ExplicitUsernameIfAny);
                sb.AppendLine("Table:" + _table.GetFullyQualifiedName());
            }

            throw new Exception("Error creating/checking Guid substitution table on:" + Environment.NewLine + sb, e);
        }
    }

    public override DiscoveredTable? GetGuidTableIfAny(IMappingTableOptions options)
    {
        return options.Discover();
    }
}
