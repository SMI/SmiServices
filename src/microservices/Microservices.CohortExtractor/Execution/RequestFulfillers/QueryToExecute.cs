using ReusableLibraryCode.DataAccess;
using FAnsi.Discovery;
using System;
using System.Collections.Generic;
using System.Data.Common;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.Spontaneous;
using Rdmp.Core.Repositories;
using Rdmp.Core.QueryBuilding;

namespace Microservices.CohortExtractor.Execution.RequestFulfillers
{
    internal class QueryToExecute
    {
        public DiscoveredServer Server { get; set; }


        private readonly string _sql;
        private readonly string _imageColumnName;
        private readonly string _seriesIdColumnName;


        public QueryToExecute(ICatalogue catalogue, ExtractionInformation keyTagColumn, ExtractionInformation filePathColumn, ExtractionInformation seriesTagColumn)
        {
            var qb = new QueryBuilder("distinct", null);
            qb.AddColumn(filePathColumn);
            qb.AddColumn(keyTagColumn);

            //Add series tag unless the KeyTag is the series tag
            if (seriesTagColumn != null)
            {
                qb.AddColumn(seriesTagColumn);
                _seriesIdColumnName = seriesTagColumn.GetRuntimeName();
            }

            var memory = new MemoryCatalogueRepository();
            var container = new SpontaneouslyInventedFilterContainer(memory,null, null, FilterContainerOperation.AND);
            container.AddChild(new SpontaneouslyInventedFilter(memory,container, keyTagColumn + "= '{0}'", "Filter Series", "Filters by series UID", null));

            

            foreach (ExtractionFilter filter in catalogue.GetAllMandatoryFilters())
                new SpontaneouslyInventedFilter(memory, container,filter.WhereSQL,filter.Name,filter.Description,filter.GetAllParameters());
            

            qb.RootFilterContainer = container;

            _sql = qb.SQL;
            _imageColumnName = filePathColumn.GetRuntimeName();

            Server = catalogue.GetDistinctLiveDatabaseServer(DataAccessContext.DataExport, false);
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
            var found = new HashSet<string>();
            string seriesID = null;

            // If null, then we are extracting by SeriesID, so just return the valueToLookup
            if (_seriesIdColumnName == null)
                seriesID = valueToLookup;

            using (DbConnection con = Server.GetConnection())
            {
                con.Open();
                DbDataReader r = Server.GetCommand(GetSqlForKeyValue(valueToLookup), con).ExecuteReader();

                while (r.Read())
                {
                    object imagePath = r[_imageColumnName];

                    if (imagePath == DBNull.Value)
                        continue;

                    if (seriesID == null)
                        seriesID = r[_seriesIdColumnName].ToString();

                    found.Add(imagePath.ToString());
                }
            }

            if (seriesID == null)
                throw new Exception("seriesID not set");

            return new Tuple<string, HashSet<string>>(seriesID, found);
        }
    }
}