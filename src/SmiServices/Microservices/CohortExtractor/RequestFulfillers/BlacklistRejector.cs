using FAnsi.Discovery;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.Spontaneous;
using Rdmp.Core.QueryBuilding;
using Rdmp.Core.Repositories;
using Rdmp.Core.ReusableLibraryCode.DataAccess;
using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace SmiServices.Microservices.CohortExtractor.RequestFulfillers
{
    public class BlacklistRejector : IRejector
    {
        private readonly ICatalogue _catalogue;
        private QueryToExecuteColumnSet? _columnSet;
        private SpontaneouslyInventedFilter? _studyFilter;
        private SpontaneouslyInventedFilter? _seriesFilter;
        private SpontaneouslyInventedFilter? _instanceFilter;
        private QueryBuilder? _queryBuilder;
        private DiscoveredServer? _server;

        public BlacklistRejector(ICatalogue catalogue)
        {
            _catalogue = catalogue;

            Initialize();
        }

        private void Initialize()
        {
            //Figure out which UID columns exist in the Catalogue, do not require file path to be in Catalogue
            _columnSet = QueryToExecuteColumnSet.Create(_catalogue, false);

            //Tells us the DBMS type
            var syntax = _catalogue.GetQuerySyntaxHelper();

            //For storing the OR container and filter(s)
            var memory = new MemoryCatalogueRepository();

            //builds SQL we will run in lookup stage
            _queryBuilder = new QueryBuilder(null, null);

            //all we care about is if the uid appears if it does then we are rejecting it
            _queryBuilder.TopX = 1;

            //Filter is OR i.e. StudyInstanceUID = @StudyInstanceUID OR SeriesInstanceUID = @SeriesInstanceUID
            var container = _queryBuilder.RootFilterContainer = new SpontaneouslyInventedFilterContainer(memory, null, null, FilterContainerOperation.OR);

            //Build SELECT and WHERE bits of the query
            if (_columnSet?.StudyTagColumn != null)
            {
                _queryBuilder.AddColumn(_columnSet.StudyTagColumn);

                string whereSql =
                    $"{_columnSet.StudyTagColumn.SelectSQL} = {syntax.ParameterSymbol}{QueryToExecuteColumnSet.DefaultStudyIdColumnName}";

                _studyFilter = new SpontaneouslyInventedFilter(memory, container, whereSql, "Study UID Filter", "", null);
                container.AddChild(_studyFilter);
            }


            if (_columnSet?.SeriesTagColumn != null)
            {
                _queryBuilder.AddColumn(_columnSet.SeriesTagColumn);

                string whereSql =
                    $"{_columnSet.SeriesTagColumn.SelectSQL} = {syntax.ParameterSymbol}{QueryToExecuteColumnSet.DefaultSeriesIdColumnName}";

                _seriesFilter = new SpontaneouslyInventedFilter(memory, container, whereSql, "Series UID Filter", "", null);
                container.AddChild(_seriesFilter);
            }

            if (_columnSet?.InstanceTagColumn != null)
            {
                _queryBuilder.AddColumn(_columnSet.InstanceTagColumn);

                string whereSql =
                    $"{_columnSet.InstanceTagColumn.SelectSQL} = {syntax.ParameterSymbol}{QueryToExecuteColumnSet.DefaultInstanceIdColumnName}";

                _instanceFilter = new SpontaneouslyInventedFilter(memory, container, whereSql, "Instance UID Filter", "", null);
                container.AddChild(_instanceFilter);
            }

            // Make sure the query builder looks valid
            if (!_queryBuilder.SelectColumns.Any())
                throw new NotSupportedException($"Blacklist Catalogue {_catalogue} (ID={_catalogue.ID}) did not have any Core ExtractionInformation columns corresponding to any of the image UID tags (e.g. StudyInstanceUID, SeriesInstanceUID, SOPInstanceUID).");

            try
            {
                // make sure we can connect to the server
                _server = _catalogue.GetDistinctLiveDatabaseServer(DataAccessContext.DataExport, true);
                _server.TestConnection();
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to test connection for Catalogue {_catalogue}", e);
            }

            // run a test lookup query against the remote database
            DoLookup("test1", "test2", "test3");
        }

        /// <summary>
        /// Looks up data stored in the Catalogue with a query matching on any of the provided uids.  All values must be supplied if the Catalogue has a column of the corresponding name (i.e. if Catalogue has SeriesInstanceUID you must supply <paramref name="seriesuid"/>)
        /// </summary>
        /// <param name="studyuid"></param>
        /// <param name="seriesuid"></param>
        /// <param name="imageuid"></param>
        /// <returns></returns>
        public bool DoLookup(string studyuid, string seriesuid, string imageuid)
        {
            string sql = _queryBuilder!.SQL;

            using var con = _server!.GetConnection();
            con.Open();
            using var cmd = _server.GetCommand(sql, con);
            //Add the current row UIDs to the parameters of the command
            if (_studyFilter != null)
                _server.AddParameterWithValueToCommand(QueryToExecuteColumnSet.DefaultStudyIdColumnName, cmd, studyuid);

            if (_seriesFilter != null)
                _server.AddParameterWithValueToCommand(QueryToExecuteColumnSet.DefaultSeriesIdColumnName, cmd, seriesuid);

            if (_instanceFilter != null)
                _server.AddParameterWithValueToCommand(QueryToExecuteColumnSet.DefaultInstanceIdColumnName, cmd, imageuid);

            using var r = cmd.ExecuteReader();
            //if we can read a record then we have an entry in the blacklist
            return r.Read();
        }

        /// <summary>
        /// Rejects the <paramref name="row"/> if it appears in the blacklisting Catalogue
        /// </summary>
        /// <param name="row"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public bool Reject(IDataRecord row, [NotNullWhen(true)] out string? reason)
        {
            //row is bad if the query matches any records (in the blacklist)
            var bad = DoLookup(
                row[QueryToExecuteColumnSet.DefaultStudyIdColumnName].ToString()!,
                row[QueryToExecuteColumnSet.DefaultSeriesIdColumnName].ToString()!,
                row[QueryToExecuteColumnSet.DefaultInstanceIdColumnName].ToString()!
            );

            reason = bad ? $"Blacklisted in {_catalogue.Name}" : null;
            return bad;
        }
    }
}
