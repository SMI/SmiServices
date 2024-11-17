using FAnsi.Discovery;
using FAnsi.Discovery.QuerySyntax;
using NLog;
using Rdmp.Core.DataLoad.Triggers;
using SmiServices.Common;
using SmiServices.Common.Helpers;
using SmiServices.Common.Messages.Updating;
using SmiServices.Common.Options;
using SmiServices.Microservices.IdentifierMapper.Swappers;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using System.Threading;


namespace SmiServices.Applications.TriggerUpdates
{
    public class MapperSource : ITriggerUpdatesSource
    {
        private readonly ISwapIdentifiers _swapper;
        private readonly TriggerUpdatesFromMapperOptions _cliOptions;
        private readonly GlobalOptions _globalOptions;

        protected readonly CancellationTokenSource TokenSource = new();
        protected readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// True if the <see cref="GetUpdates"/> database querying operation is currently executing
        /// </summary>
        public bool IsExecuting { get; private set; } = false;

        private DbCommand? _currentCommandMainTable;
        private DbCommand? _currentCommandOtherTables;

        public MapperSource(GlobalOptions globalOptions, TriggerUpdatesFromMapperOptions cliOptions)
        {
            _cliOptions = cliOptions;
            _globalOptions = globalOptions;

            FansiImplementations.Load();

            ISwapIdentifiers? swapper;
            try
            {
                var objectFactory = new MicroserviceObjectFactory();
                swapper = objectFactory.CreateInstance<ISwapIdentifiers>(globalOptions.IdentifierMapperOptions!.SwapperType!, typeof(ISwapIdentifiers).Assembly);
            }
            catch (Exception ex)
            {
                throw new Exception($"Could not create IdentifierMapper Swapper with SwapperType:{globalOptions.IdentifierMapperOptions?.SwapperType ?? "Null"}", ex);
            }

            _swapper = swapper ?? throw new ArgumentException("No SwapperType has been specified in GlobalOptions.IdentifierMapperOptions");
        }

        public IEnumerable<UpdateValuesMessage> GetUpdates()
        {
            IsExecuting = true;

            try
            {
                var mappingTable = _globalOptions.IdentifierMapperOptions!.Discover();

                if (!mappingTable.Exists())
                    throw new Exception($"Mapping table {mappingTable.GetFullyQualifiedName()} did not exist");

                var syntaxHelper = mappingTable.GetQuerySyntaxHelper();

                var archiveTableName = mappingTable.GetRuntimeName() + "_Archive";
                var archiveTable = mappingTable.Database.ExpectTable(syntaxHelper.EnsureWrapped(archiveTableName), schema: mappingTable.Schema);

                //may be null!
                var guidTable = _swapper.GetGuidTableIfAny(_globalOptions.IdentifierMapperOptions);

                if (!archiveTable.Exists())
                    throw new Exception($"No Archive table exists for mapping table {mappingTable.GetFullyQualifiedName()}");

                var swapCol = _globalOptions.IdentifierMapperOptions.SwapColumnName!;
                var forCol = _globalOptions.IdentifierMapperOptions.ReplacementColumnName!;

                // may be null!
                var liveDatabaseFieldName = _cliOptions.LiveDatabaseFieldName;

                var archiveFetchSql = GetArchiveFetchSql(archiveTable, swapCol, forCol);

                using var con = mappingTable.Database.Server.GetConnection();
                con.Open();

                var dateOfLastUpdate = _cliOptions.DateOfLastUpdate;

                //find all records in the table that are new
                var cmd = mappingTable.GetCommand($"SELECT {syntaxHelper.EnsureWrapped(swapCol)}, {syntaxHelper.EnsureWrapped(forCol)} FROM {mappingTable.GetFullyQualifiedName()} WHERE {syntaxHelper.EnsureWrapped(SpecialFieldNames.ValidFrom)} >= @dateOfLastUpdate", con);
                cmd.CommandTimeout = _globalOptions.TriggerUpdatesOptions!.CommandTimeoutInSeconds;

                mappingTable.Database.Server.AddParameterWithValueToCommand("@dateOfLastUpdate", cmd, dateOfLastUpdate);

                _currentCommandMainTable = cmd;

                TokenSource.Token.ThrowIfCancellationRequested();

                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    TokenSource.Token.ThrowIfCancellationRequested();

                    var currentSwapColValue = r[swapCol];
                    var currentForColValue = r[forCol];

                    //if there is a corresponding record in the archive table
                    using (var con2 = archiveTable.Database.Server.GetConnection())
                    {
                        con2.Open();
                        var cmd2 = archiveTable.GetCommand(archiveFetchSql, con2);
                        cmd2.CommandTimeout = _globalOptions.TriggerUpdatesOptions.CommandTimeoutInSeconds;
                        _currentCommandOtherTables = cmd2;

                        archiveTable.Database.Server.AddParameterWithValueToCommand("@currentSwapColValue", cmd2, currentSwapColValue);

                        var oldForColValue = cmd2.ExecuteScalar();

                        TokenSource.Token.ThrowIfCancellationRequested();

                        //if there is an entry in the archive for this old one then it is not a brand new record i.e. it is an update
                        if (oldForColValue != null)
                        {
                            //there is an entry in the archive so we need to issue a database update to update the live tables so the old archive
                            // table swap value (e.g. ECHI) is updated to the new one in the live table
                            yield return new UpdateValuesMessage
                            {
                                WhereFields = [liveDatabaseFieldName ?? forCol],
                                HaveValues = [Qualify(oldForColValue)],

                                WriteIntoFields = [liveDatabaseFieldName ?? forCol],
                                Values = [Qualify(currentForColValue)]
                            };
                        }
                    }

                    TokenSource.Token.ThrowIfCancellationRequested();

                    // We should also look at guid mappings that are filled in now because of brand new records
                    if (guidTable != null)
                    {
                        string guidFetchSql = $"SELECT {syntaxHelper.EnsureWrapped(TableLookupWithGuidFallbackSwapper.GuidColumnName)} FROM {guidTable.GetFullyQualifiedName()} WHERE {syntaxHelper.EnsureWrapped(swapCol)}=@currentSwapColValue";

                        using var con3 = guidTable.Database.Server.GetConnection();
                        con3.Open();
                        var cmd3 = guidTable.GetCommand(guidFetchSql, con3);
                        cmd3.CommandTimeout = _globalOptions.TriggerUpdatesOptions.CommandTimeoutInSeconds;
                        _currentCommandOtherTables = cmd3;

                        guidTable.Database.Server.AddParameterWithValueToCommand("@currentSwapColValue", cmd3, currentSwapColValue);

                        var oldTemporaryMapping = cmd3.ExecuteScalar();

                        TokenSource.Token.ThrowIfCancellationRequested();

                        //if this brand new mapping has a temporary guid assigned to it we need to issue an update of the temporary guid to the legit new mapping
                        if (oldTemporaryMapping != null)
                        {
                            yield return new UpdateValuesMessage
                            {
                                WhereFields = [liveDatabaseFieldName ?? forCol],
                                HaveValues = [Qualify(oldTemporaryMapping)],

                                WriteIntoFields = [liveDatabaseFieldName ?? forCol],
                                Values = [Qualify(currentForColValue)]
                            };
                        }
                    }
                }
            }
            finally
            {
                IsExecuting = false;
            }
        }

        /// <summary>
        /// Returns DBMS formatted representation for constant <paramref name="value"/>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private string Qualify(object value)
        {
            if (value == DBNull.Value || string.IsNullOrWhiteSpace(value?.ToString()))
                return "null";

            if (_cliOptions.Qualifier != '\0')
                return _cliOptions.Qualifier + value.ToString() + _cliOptions.Qualifier;

            return value.ToString() ?? throw new ArgumentException("Couldn't convert value to string");
        }

        /// <summary>
        /// Returns a query for fetching the latest entry in the archive that matches a given private identifier (query contains parameter @currentSwapColValue)
        /// </summary>
        /// <param name="archiveTable"></param>
        /// <param name="swapCol">The private identifier column name e.g. CHI</param>
        /// <param name="forCol">The public release identifier column name e.g. ECHI</param>
        /// <returns>SQL for fetching the latest release identifier value (e.g. ECHI value) from the archive</returns>
        private static string GetArchiveFetchSql(DiscoveredTable archiveTable, string swapCol, string forCol)
        {
            // Work out how to get the latest entry in the _Archive table that corresponds to a given private identifier (e.g. CHI)
            var syntax = archiveTable.Database.Server.GetQuerySyntaxHelper();

            var topX = syntax.HowDoWeAchieveTopX(1);

            StringBuilder sb = new();
            sb.AppendLine("SELECT ");

            if (topX.Location == QueryComponent.SELECT)
                sb.AppendLine(topX.SQL);

            sb.AppendLine(syntax.EnsureWrapped(forCol));
            sb.AppendLine("FROM " + archiveTable.GetFullyQualifiedName());
            sb.AppendLine("WHERE");
            sb.AppendLine($"{syntax.EnsureWrapped(swapCol)} = @currentSwapColValue");

            if (topX.Location == QueryComponent.WHERE)
            {
                sb.AppendLine("AND");
                sb.AppendLine(topX.SQL);
            }

            sb.AppendLine("ORDER BY");
            sb.AppendLine(syntax.EnsureWrapped(SpecialFieldNames.ValidFrom) + " desc");

            if (topX.Location == QueryComponent.Postfix)
                sb.AppendLine(topX.SQL);

            return sb.ToString();
        }

        public void Stop()
        {
            TokenSource.Cancel();

            _currentCommandMainTable?.Cancel();

            _currentCommandOtherTables?.Cancel();

            // give application 10 seconds to exit
            var timeout = 10_000;
            const int delta = 500;
            while (IsExecuting && timeout > 0)
            {
                Thread.Sleep(delta);
                timeout -= delta;
            }

            if (timeout <= 0)
                throw new ApplicationException("Query execution did not exit in time");

            Logger.Info("Query execution aborted, exiting");
        }
    }
}
