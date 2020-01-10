using ReusableLibraryCode.DataAccess;
using FAnsi.Discovery;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using MapsDirectlyToDatabaseTable;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.Spontaneous;
using Rdmp.Core.Repositories;
using Rdmp.Core.QueryBuilding;

namespace Microservices.CohortExtractor.Execution.RequestFulfillers
{
    public class QueryToExecute
    {
        /// <summary>
        /// The dataset to query
        /// </summary>
        protected readonly ICatalogue Catalogue;

        /// <summary>
        /// The value to look up in the <see cref="Catalogue"/>
        /// </summary>
        protected readonly ExtractionInformation KeyTagColumn;

        /// <summary>
        /// The column in the <see cref="Catalogue"/> that stores the location on disk of the image
        /// </summary>
        protected readonly ExtractionInformation FilePathColumn;

        /// <summary>
        /// The column in the <see cref="Catalogue"/> that stores the Series UID
        /// </summary>
        protected readonly ExtractionInformation SeriesTagColumn;

        public DiscoveredServer Server { get; set; }
        private string _sql;
        
        /// <summary>
        /// Lock to ensure we don't build multiple <see cref="GetQueryBuilder"/> at once if someone decides to multi
        /// thread the <see cref="Execute"/> method
        /// </summary>
        readonly object _oLockExecute = new object();

        public QueryToExecute(ICatalogue catalogue, ExtractionInformation keyTagColumn, ExtractionInformation filePathColumn, ExtractionInformation seriesTagColumn)
        {
            Catalogue = catalogue;
            KeyTagColumn = keyTagColumn;
            FilePathColumn = filePathColumn;
            SeriesTagColumn = seriesTagColumn;
            
            Server = catalogue.GetDistinctLiveDatabaseServer(DataAccessContext.DataExport, false);
        }

        /// <summary>
        /// Creates a query builder with all the columns required to match rows on the
        /// <see cref="KeyTagColumn"/>
        /// </summary>
        /// <returns></returns>
        protected virtual QueryBuilder GetQueryBuilder()
        {
            var qb = new QueryBuilder("distinct", null);
            
            foreach (var col in GetColumns()) 
                qb.AddColumn(col);

            qb.RootFilterContainer = GetWhereLogic();

            return qb;
        }

        /// <summary>
        /// Override to change what columns are brought back from the <see cref="Catalogue"/>.  Defaults to
        /// <see cref="FilePathColumn"/>, <see cref="KeyTagColumn"/> and <see cref="SeriesTagColumn"/> (unless
        /// it is the  <see cref="KeyTagColumn"/>).
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerable<IColumn> GetColumns()
        {
            yield return FilePathColumn;
            yield return KeyTagColumn;

            if(!Equals(KeyTagColumn,SeriesTagColumn))
                yield return SeriesTagColumn;
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
        /// <see cref="KeyTagColumn"/> and AND with all <see cref="ICatalogue.GetAllMandatoryFilters"/> listed on the <see cref="Catalogue"/>
        /// </summary>
        /// <param name="memoryRepo"></param>
        /// <param name="rootContainer"></param>
        /// <returns></returns>
        protected virtual IEnumerable<IFilter> GetFilters(MemoryCatalogueRepository memoryRepo,IContainer rootContainer)
        {
            yield return new SpontaneouslyInventedFilter(memoryRepo, rootContainer, KeyTagColumn + "= '{0}'",
                "Filter Series", "Filters by series UID", null);

            foreach(var filter in Catalogue.GetAllMandatoryFilters())
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
        /// <returns></returns>
        public Tuple<string, HashSet<string>> Execute(string valueToLookup)
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
            
            var found = new HashSet<string>();
            string seriesId = null;

            using (DbConnection con = Server.GetConnection())
            {
                con.Open();
                DbDataReader r = Server.GetCommand(GetSqlForKeyValue(valueToLookup), con).ExecuteReader();

                while (r.Read())
                {
                    object imagePath = r[FilePathColumn.GetRuntimeName()];

                    if (imagePath == DBNull.Value)
                        continue;

                    seriesId = r[SeriesTagColumn.GetRuntimeName()].ToString();

                    found.Add(imagePath.ToString());
                }
            }

            if (seriesId == null)
                throw new Exception("seriesID not set");

            return new Tuple<string, HashSet<string>>(seriesId, found);
        }
    }
}