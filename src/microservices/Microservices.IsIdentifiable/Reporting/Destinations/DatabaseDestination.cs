using System;
using System.Data;
using FAnsi.Discovery;
using Microservices.IsIdentifiable.Options;

namespace Microservices.IsIdentifiable.Reporting.Destinations
{
    internal class DatabaseDestination : ReportDestination
    {
        private readonly string _reportName;
        private DiscoveredTable _tbl;

        public DatabaseDestination(IsIdentifiableAbstractOptions options, string reportName)
            : base(options)
        {
            var targetDatabase = new DiscoveredServer(options.DestinationConnectionString, options.DestinationDatabaseType).GetCurrentDatabase();

            if (!targetDatabase.Exists())
                throw new Exception("Destination database did not exist");

            _tbl = targetDatabase.ExpectTable(reportName);

            if (_tbl.Exists())
                _tbl.Drop();

            _reportName = reportName;
        }

        public override void WriteItems(DataTable items)
        {
            StripWhiteSpace(items);

            items.TableName = _reportName;
            
            if (!_tbl.Exists())
                _tbl.Database.CreateTable(_tbl.GetRuntimeName(), items);
            else
            {
                using (var insert = _tbl.BeginBulkInsert())
                {
                    insert.Upload(items);
                }
            }
        }

        private void StripWhiteSpace(DataTable items)
        {
            if (!Options.DestinationNoWhitespace)
                return;

            foreach (DataRow row in items.Rows)
                foreach (DataColumn col in items.Columns)
                    row[col] = StripWhitespace(row[col]);
        }
    }
}
