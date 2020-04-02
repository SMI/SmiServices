using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using CsvHelper;
using Microservices.IsIdentifiable.Options;
using NLog;

namespace Microservices.IsIdentifiable.Reporting.Destinations
{
    public class CsvDestination : ReportDestination
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        private string _reportPath;
        private CsvWriter _csvWriter;

        private readonly object _oHeaderLock = new object();
        private bool _headerWritten;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <param name="reportName"></param>
        /// <param name="addTimestampToFilename">True to add the time to the CSV filename generated</param>
        public CsvDestination(IsIdentifiableAbstractOptions options, string reportName,bool addTimestampToFilename = true)
            : base(options)
        {
            var destDir = new DirectoryInfo(Options.DestinationCsvFolder);

            if (!destDir.Exists)
                destDir.Create();

            _reportPath = addTimestampToFilename ?
                Path.Combine(destDir.FullName, DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm") + "-" + reportName + ".csv") : 
                Path.Combine(destDir.FullName, reportName + ".csv");
        }

        public CsvDestination(IsIdentifiableAbstractOptions options, FileInfo file):base(options)
        {
            _reportPath = file.FullName;
        }

        public override void WriteHeader(params string[] headers)
        {
            lock (_oHeaderLock)
            {
                if (_headerWritten)
                    return;

                _headerWritten = true;

                var csvFile = new FileInfo(_reportPath);
                _csvWriter = new CsvWriter(new StreamWriter(csvFile.FullName),System.Globalization.CultureInfo.CurrentCulture);

                // If there is an overriding separator and it's not a comma, then use the users desired delimiter string
                string sep = Options.DestinationCsvSeparator;
                if (!string.IsNullOrWhiteSpace(sep) && !sep.Trim().Equals(","))
                {
                    _csvWriter.Configuration.Delimiter =
                        sep.Replace("\\t", "\t").Replace("\\r", "\r").Replace("\\n", "\n");
                    _csvWriter.Configuration.ShouldQuote = (s,m) => false;

                }

                WriteRow(headers);
            }
        }

        public override void WriteItems(DataTable items)
        {
            if (!_headerWritten)
                WriteHeader((from dc in items.Columns.Cast<DataColumn>() select dc.ColumnName).ToArray());

            foreach (DataRow row in items.Rows)
                WriteRow(row.ItemArray);
        }

        public override void Dispose()
        {
            _csvWriter.Dispose();
        }

        private void WriteRow(IEnumerable<object> rowItems)
        {
            foreach (string item in rowItems)
                _csvWriter.WriteField(StripWhitespace(item));

            _csvWriter.NextRecord();
        }
    }
}
