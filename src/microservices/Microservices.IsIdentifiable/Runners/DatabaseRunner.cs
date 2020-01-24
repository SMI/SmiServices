using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using FAnsi.Discovery;
using Microservices.IsIdentifiable.Failures;
using Microservices.IsIdentifiable.Options;
using Microservices.IsIdentifiable.Reporting;
using NLog;

namespace Microservices.IsIdentifiable.Runners
{
    class DatabaseRunner : IsIdentifiableAbstractRunner
    {
        private readonly IsIdentifiableRelationalDatabaseOptions _opts;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private DiscoveredColumn[] _columns;
        private string[] _columnsNames;
        private bool[] _stringColumns;
        private DatabaseFailureFactory _factory;

        public DatabaseRunner(IsIdentifiableRelationalDatabaseOptions opts) : base(opts)
        {
            _opts = opts;
        }

        public override int Run()
        {
            DiscoveredTable tbl = GetServer(_opts.DatabaseConnectionString, _opts.DatabaseType, _opts.TableName);
            var server = tbl.Database.Server;

            _factory = new DatabaseFailureFactory(tbl);

            _columns = tbl.DiscoverColumns();
            _columnsNames = _columns.Select(c => c.GetRuntimeName()).ToArray();
            _stringColumns = _columns.Select(c => c.GetGuesser().Guess.CSharpType == typeof(string)).ToArray();

            using (var con = server.GetConnection())
            {
                con.Open();

                var cmd = server.GetCommand(
                    string.Format("SELECT {0} FROM {1}"
                    , string.Join("," + Environment.NewLine, _columns.Select(c => c.GetFullyQualifiedName()).ToArray())
                    , tbl.GetFullyQualifiedName()), con);

                _logger.Info("About to send command:" + Environment.NewLine + cmd.CommandText);

                var reader = cmd.ExecuteReader();

                foreach (Reporting.Failure failure in reader.Cast<DbDataRecord>().AsParallel().SelectMany(GetFailuresIfAny))
                    AddToReports(failure);

                CloseReports();
            }
            return 0;
        }

        private IEnumerable<Reporting.Failure> GetFailuresIfAny(DbDataRecord record)
        {
            //Get the primary key of the current row
            string[] primaryKey = _factory.PrimaryKeys.Select(k => record[k.GetRuntimeName()].ToString()).ToArray();

            //For each column in the table
            for (var i = 0; i < _columnsNames.Length; i++)
                //If it is a string column
                if (_stringColumns[i])
                {
                    var asString = record[i] as string;

                    if (string.IsNullOrWhiteSpace(asString))
                        continue;

                    var parts = new List<FailurePart>();

                    foreach (string part in asString.Split('\\'))
                        parts.AddRange(Validate(_columnsNames[i], part));

                    if (parts.Any())
                        yield return _factory.Create(_columnsNames[i], asString, parts, primaryKey);
                }

            DoneRows(1);
        }
    }
}
