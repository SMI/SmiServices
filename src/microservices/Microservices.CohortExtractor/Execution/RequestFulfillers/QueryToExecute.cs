using FAnsi.Discovery;
using MySqlConnector;
using NLog;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.Spontaneous;
using Rdmp.Core.QueryBuilding;
using Rdmp.Core.Repositories;
using ReusableLibraryCode.DataAccess;
using System;
using System.Collections.Generic;
using System.Data.Common;

namespace Microservices.CohortExtractor.Execution.RequestFulfillers
{
    public class QueryToExecute
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        protected QueryToExecuteColumnSet Columns { get; }

        /// <summary>
        /// The column to search for in the WHERE logic
        /// </summary>
        public string KeyTag { get; }

        public DiscoveredServer Server { get; set; }
        private string _sql;
        
        /// <summary>
        /// Lock to ensure we don't build multiple <see cref="GetQueryBuilder"/> at once if someone decides to multi
        /// thread the <see cref="Execute"/> method
        /// </summary>
        readonly object _oLockExecute = new();

        /// <summary>
        /// Modality (if known) for records that the query should return
        /// </summary>
        public string Modality { get; set; }

        public QueryToExecute(QueryToExecuteColumnSet columns, string keyTag)
        {
            Columns = columns;
            KeyTag = keyTag;
            Server = columns.Catalogue.GetDistinctLiveDatabaseServer(DataAccessContext.DataExport, false);
        }

        /// <summary>
        /// Creates a query builder with all the columns required to match rows on the
        /// </summary>
        /// <returns></returns>
        protected virtual QueryBuilder GetQueryBuilder()
        {
            var qb = new QueryBuilder("distinct", null);
            
            foreach (var col in Columns.AllColumns) 
                qb.AddColumn(col);

            qb.RootFilterContainer = GetWhereLogic();

            return qb;
        }
        
        /// <summary>
        /// Generates the WHERE logic for the query.  Adds a single root container with AND operation and then adds
        /// all filters in <see cref="GetFilters"/>.  It is better to override <see cref="GetFilters"/> unless you want
        /// to create a nested container tree for the query.
        /// </summary>
        /// <returns></returns>
        protected virtual IContainer GetWhereLogic()
        {
            //make a root WHERE container in memory
            var memory = new MemoryCatalogueRepository();
            var container = new SpontaneouslyInventedFilterContainer(memory,null, null, FilterContainerOperation.AND);
            
            //Get all filters that we are to add and add them to the root
            foreach (IFilter filter in GetFilters(memory,container))
                container.AddChild(new SpontaneouslyInventedFilter(memory, container,filter.WhereSQL,filter.Name,filter.Description,filter.GetAllParameters()));

            return container;
        }

        /// <summary>
        /// Override to change what filters are included in the WHERE Sql of your query.  Default behaviour is to match on the
        /// <see cref="KeyTag"/> and AND with all <see cref="ICatalogue.GetAllMandatoryFilters"/> listed on the <see cref="Catalogue"/>
        /// </summary>
        /// <param name="memoryRepo"></param>
        /// <param name="rootContainer"></param>
        /// <returns></returns>
        protected virtual IEnumerable<IFilter> GetFilters(MemoryCatalogueRepository memoryRepo,IContainer rootContainer)
        {
            yield return new SpontaneouslyInventedFilter(memoryRepo, rootContainer, KeyTag + "= '{0}'",
                "Filter Series", "Filters by series UID", null);

            foreach(var filter in Columns.Catalogue.GetAllMandatoryFilters())
                yield return filter;
        }

        private string GetSqlForKeyValue(string value)
        {
            return string.Format(_sql, value);
        }
        
        /// <summary>
        /// Returns the SeriesInstanceUID and a set of any file paths matching the query
        /// </summary>
        /// <param name="valueToLookup"></param>
        /// <param name="rejectors">Required, determines whether records are returned as good or bad</param>
        /// <returns></returns>
        public IEnumerable<QueryToExecuteResult> Execute(string valueToLookup, List<IRejector> rejectors)
        {
            if(_sql == null)
                lock (_oLockExecute)
                {
                    if (_sql == null)
                    {
                        var qb = GetQueryBuilder();
                        _sql = qb.SQL;
                    }        
                }
            
            var path = Columns.FilePathColumn.GetRuntimeName();
            var study = Columns.StudyTagColumn?.GetRuntimeName();
            var series = Columns.SeriesTagColumn?.GetRuntimeName();
            var instance = Columns.InstanceTagColumn?.GetRuntimeName();

            using DbConnection con = Server.GetConnection();
            con.Open();

            string sqlString = GetSqlForKeyValue(valueToLookup);

            DbDataReader reader;
            try
            {
                reader = Server.GetCommand(sqlString, con).ExecuteReader();
            }
            catch (MySqlException)
            {
                _logger.Error($"The following query resulted in an exception: {sqlString}");
                throw;
            }

            while (reader.Read())
            {
                object imagePath = reader[path];

                if (imagePath == DBNull.Value)
                    continue;

                bool reject = false;
                string rejectReason = null;

                //Ask the rejectors how good this record is
                foreach (IRejector rejector in rejectors)
                {
                    if (rejector.Reject(reader, out rejectReason))
                    {
                        reject = true;
                        break;
                    }
                }

                yield return
                    new QueryToExecuteResult((string)imagePath,
                        study == null ? null : (string)reader[study],
                        series == null ? null : (string)(string)reader[series],
                        instance == null ? null : (string)(string)reader[instance],
                        reject,
                        rejectReason);
            }
        }
    }
}
