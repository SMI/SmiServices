using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using FAnsi.Discovery;
using MapsDirectlyToDatabaseTable;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.Spontaneous;
using Rdmp.Core.QueryBuilding;
using Rdmp.Core.Repositories;
using ReusableLibraryCode.DataAccess;

namespace Microservices.CohortExtractor.Execution.RequestFulfillers
{
    public class BlacklistRejector : IRejector
    {
        private readonly ICatalogue _catalogue;
        private QueryToExecuteColumnSet _columnSet;
        private SpontaneouslyInventedFilter _studyFilter;
        private SpontaneouslyInventedFilter _seriesFilter;
        private SpontaneouslyInventedFilter _instanceFilter;
        private QueryBuilder _queryBuilder;
        private DiscoveredServer _server;

        public BlacklistRejector(ICatalogue catalogue)
        {
            _catalogue = catalogue;

            Initialize();
        }

        private void Initialize()
        {
            _columnSet = QueryToExecuteColumnSet.Create(_catalogue,false);
            var syntax = _catalogue.GetQuerySyntaxHelper();

            var memory = new MemoryCatalogueRepository();
            
            _queryBuilder = new QueryBuilder(null,null);
            
            //all we care about is if the uid appears if it does then we are rejecting it
            _queryBuilder.TopX = 1;

            var container = _queryBuilder.RootFilterContainer = new SpontaneouslyInventedFilterContainer(memory,null,null,FilterContainerOperation.OR);

            //Build SELECT and WHERE columns
            if (_columnSet.StudyTagColumn != null)
            {
                _queryBuilder.AddColumn(_columnSet.StudyTagColumn);

                string whereSql =
                    $"{_columnSet.StudyTagColumn.SelectSQL} = {syntax.ParameterSymbol}{QueryToExecuteColumnSet.DefaultStudyIdColumnName}";
                    
                _studyFilter = new SpontaneouslyInventedFilter(memory,container,whereSql, "Series UID Filter","",null);
                container.AddChild(_studyFilter);
            }
                

            if(_columnSet.SeriesTagColumn != null)
            {
                _queryBuilder.AddColumn(_columnSet.SeriesTagColumn);

                string whereSql =
                    $"{_columnSet.SeriesTagColumn.SelectSQL} = {syntax.ParameterSymbol}{QueryToExecuteColumnSet.DefaultSeriesIdColumnName}";
                    
                _seriesFilter = new SpontaneouslyInventedFilter(memory,container,whereSql, "Series UID Filter","",null);
                container.AddChild(_seriesFilter);
            }

            if (_columnSet.InstanceTagColumn != null)
            {
                _queryBuilder.AddColumn(_columnSet.InstanceTagColumn);
                
                string whereSql =
                    $"{_columnSet.InstanceTagColumn.SelectSQL} = {syntax.ParameterSymbol}{QueryToExecuteColumnSet.DefaultInstanceIdColumnName}";
                    
                _instanceFilter = new SpontaneouslyInventedFilter(memory,container,whereSql, "Instance UID Filter","",null);
                container.AddChild(_instanceFilter);
            }

            if(!_queryBuilder.SelectColumns.Any())
                throw new NotSupportedException($"Blacklist Catalogue {_catalogue} (ID={_catalogue.ID}) did not have any Core ExtractionInformation columns corresponding to any of the image UID tags (e.g. StudyInstanceUID, SeriesInstanceUID, SOPInstanceUID).");

            try
            {
                _server = _catalogue.GetDistinctLiveDatabaseServer(DataAccessContext.DataExport, true);
                _server.TestConnection();
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to test connection for Catalogue {_catalogue}",e);
            }

            DoLookup("test1", "test2", "test3");            
        }

        public bool DoLookup(string studyuid, string seriesuid, string imageuid)
        {
            string sql = _queryBuilder.SQL;
            
            using (var con = _server.GetConnection())
            {
                con.Open();
                using (var cmd = _server.GetCommand(sql, con))
                {
                    if(_studyFilter != null)
                        _server.AddParameterWithValueToCommand(QueryToExecuteColumnSet.DefaultStudyIdColumnName, cmd, studyuid);

                    if(_seriesFilter != null)
                        _server.AddParameterWithValueToCommand(QueryToExecuteColumnSet.DefaultSeriesIdColumnName, cmd, seriesuid);

                    if(_instanceFilter != null)
                        _server.AddParameterWithValueToCommand(QueryToExecuteColumnSet.DefaultInstanceIdColumnName, cmd, imageuid);

                    using (var r = cmd.ExecuteReader())
                    {
                        return r.Read();
                    }
                }
            }
        }

        public bool Reject(DbDataReader row, out string reason)
        {
            //row is bad if the query matches any records (in the blacklist)
            var bad = DoLookup(
                row[QueryToExecuteColumnSet.DefaultStudyIdColumnName].ToString(),
                row[QueryToExecuteColumnSet.DefaultSeriesIdColumnName].ToString(),
                row[QueryToExecuteColumnSet.DefaultInstanceIdColumnName].ToString()
            );

            reason = bad ? $"Blacklisted in {_catalogue.Name}" : null;
            return bad;
        }
    }
}
