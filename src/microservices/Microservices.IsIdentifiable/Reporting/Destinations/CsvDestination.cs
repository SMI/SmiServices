using CsvHelper;
using CsvHelper.Configuration;
using Microservices.IsIdentifiable.Options;
using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Abstractions;
using System.Linq;

namespace Microservices.IsIdentifiable.Reporting.Destinations
{
    public class CsvDestination : ReportDestination
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        private string _reportPath;
        private StreamWriter _streamwriter;
        private CsvWriter _csvWriter;

        private readonly object _oHeaderLock = new object();
        private bool _headerWritten;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <param name="reportName"></param>
        /// <param name="addTimestampToFilename">True to add the time to the CSV filename generated</param>
        /// <param name="fileSystem"></param>
        public CsvDestination(IsIdentifiableAbstractOptions options, string reportName, bool addTimestampToFilename = true, IFileSystem fileSystem = null)
            : base(options, fileSystem)
        {
            var destDir = FileSystem.DirectoryInfo.FromDirectoryName(Options.DestinationCsvFolder);

            if (!destDir.Exists)
                destDir.Create();

            _reportPath = addTimestampToFilename ?
                FileSystem.Path.Combine(destDir.FullName, DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm") + "-" + reportName + ".csv") :
                FileSystem.Path.Combine(destDir.FullName, reportName + ".csv");
        }

        public CsvDestination(IsIdentifiableAbstractOptions options, IFileInfo file) : base(options)
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

                var csvFile = FileSystem.FileInfo.FromFileName(_reportPath);
                CsvConfiguration csvconf;
                string sep = Options.DestinationCsvSeparator;
                // If there is an overriding separator and it's not a comma, then use the users desired delimiter string
                if (!string.IsNullOrWhiteSpace(sep) && !sep.Trim().Equals(","))
                {
                    csvconf = new CsvConfiguration(System.Globalization.CultureInfo.CurrentCulture)
                    {
                        Delimiter =
                            sep.Replace("\\t", "\t").Replace("\\r", "\r").Replace("\\n", "\n"),
                        ShouldQuote = _ => false,
                    };
                }
                else
                {
                    csvconf = new CsvConfiguration(System.Globalization.CultureInfo.CurrentCulture);
                }

                var stream = csvFile.OpenWrite();
                _streamwriter = new StreamWriter(stream);
                _csvWriter = new CsvWriter(_streamwriter, csvconf);
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
            _csvWriter?.Dispose();
            _streamwriter?.Dispose();
        }

        private void WriteRow(IEnumerable<object> rowItems)
        {
            foreach (string item in rowItems)
                _csvWriter.WriteField(StripWhitespace(item));

            _csvWriter.NextRecord();
        }
    }
}
