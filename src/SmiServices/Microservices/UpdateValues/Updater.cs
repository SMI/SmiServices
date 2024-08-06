using FAnsi.Discovery;
using NLog;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Repositories;
using SmiServices.Common.Messages.Updating;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SmiServices.Microservices.UpdateValues
{
    public class Updater : IUpdater
    {
        private readonly ICatalogueRepository _repository;

        /// <summary>
        /// Number of seconds the updater will wait when running a single value UPDATE on the live table e.g. ECHI A needs to be replaced with ECHI B
        /// </summary>
        public int UpdateTimeout { get; set; } = 1000000;

        /// <summary>
        /// List of IDs of <see cref="TableInfo"/> that should be examined for update potential.  If blank/empty then all tables will be considered.
        /// </summary>
        public int[] TableInfosToUpdate { get; internal set; } = Array.Empty<int>();

        ConcurrentDictionary<DiscoveredTable, UpdateTableAudit> _audits { get; } = new ConcurrentDictionary<DiscoveredTable, UpdateTableAudit>();

        public Updater(ICatalogueRepository repository)
        {
            _repository = repository;
        }

        public int HandleUpdate(UpdateValuesMessage message)
        {
            message.Validate();

            TableInfo[] tables;
            var affectedRows = 0;

            if (message.ExplicitTableInfo.Length != 0)
            {
                tables = _repository.GetAllObjectsInIDList<TableInfo>(message.ExplicitTableInfo).ToArray();

                if (tables.Length != message.ExplicitTableInfo.Length)
                {
                    throw new Exception($"Could not find all TableInfos IDs={string.Join(",", message.ExplicitTableInfo)}.  Found {tables.Length}:{string.Join(",", tables.Select(t => t.ID))}");
                }
            }
            else
            {
                tables = GetAllTables(message.WhereFields.Union(message.WriteIntoFields).ToArray()).ToArray();

                if (tables.Length == 0)
                    throw new Exception($"Could not find any tables to update that matched the field set {message}");
            }

            //don't try to update views
            foreach (var tbl in tables.Where(static t => !t.IsView).Select(static t =>
                         t.Discover(Rdmp.Core.ReusableLibraryCode.DataAccess.DataAccessContext.DataLoad)))
            {
                if (!tbl.Exists())
                    throw new Exception($"Table {tbl} did not exist");

                affectedRows += UpdateTable(tbl, message);
            }

            return affectedRows;
        }

        /// <summary>
        /// Generates and runs an SQL command on <paramref name="t"/>
        /// </summary>
        /// <param name="t"></param>
        /// <param name="message"></param>
        protected virtual int UpdateTable(DiscoveredTable t, UpdateValuesMessage message)
        {
            var audit = _audits.GetOrAdd(t, static k => new UpdateTableAudit(k));

            var builder = new StringBuilder($"UPDATE {t.GetFullyQualifiedName()} SET ");
            builder.AppendJoin(',',
                message.WriteIntoFields.Select((field, i) =>
                    GetFieldEqualsValueExpression(t.DiscoverColumn(message.WriteIntoFields[i]), message.Values[i],
                        "=")));

            builder.AppendLine(" WHERE ");

            builder.AppendJoin(" AND ", message.WhereFields.Select((field, i) =>
                GetFieldEqualsValueExpression(t.DiscoverColumn(field ?? throw new ArgumentNullException(nameof(field))),
                    message.HaveValues[i]!,
                    message.Operators?[i])));

            var sql = builder.ToString();
            var affectedRows = 0;

            audit.StartOne();
            try
            {
                using var con = t.Database.Server.GetConnection();
                con.Open();

                using var cmd = t.Database.Server.GetCommand(sql, con);
                cmd.CommandTimeout = UpdateTimeout;

                try
                {
                    return affectedRows = cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to execute query {sql} ", ex);
                }
            }
            finally
            {
                audit.EndOne(affectedRows < 0 ? 0 : affectedRows);
            }
        }

        /// <summary>
        /// Returns the SQL string <paramref name="col"/>=<paramref name="value"/>
        /// </summary>
        /// <param name="col">LHS argument</param>
        /// <param name="value">RHS argument, if null then string literal "null" is used</param>
        /// <param name="op">The SQL operator to use, if null "=" is used</param>
        /// <returns></returns>
        protected string GetFieldEqualsValueExpression(DiscoveredColumn col, string value, string? op)
        {
            StringBuilder builder = new();

            builder.Append(col.GetFullyQualifiedName());
            builder.Append(' ');
            builder.Append(op ?? "=");
            builder.Append(' ');

            builder.Append(string.IsNullOrWhiteSpace(value) ? "null" : value);

            return builder.ToString();
        }

        /// <summary>
        /// Returns all <see cref="TableInfo"/> which have all the <paramref name="fields"/> listed
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        protected virtual IEnumerable<TableInfo> GetAllTables(string?[] fields)
        {
            //the tables we should consider
            var tables = TableInfosToUpdate.Any() ?
                            _repository.GetAllObjectsInIDList<TableInfo>(TableInfosToUpdate) :
                            _repository.GetAllObjects<TableInfo>();

            // get only those that have all the WHERE/SET columns in them
            return tables.Where(t => fields.All(f => t.ColumnInfos.Select(static c => c.GetRuntimeName()).Contains(f)));
        }


        internal void LogProgress(ILogger logger, LogLevel level)
        {
            // ToArray prevents modification during enumeration possibility
            foreach (var audit in _audits.Values.ToArray())
            {
                logger.Log(level, audit.ToString());
            }
        }
    }
}
