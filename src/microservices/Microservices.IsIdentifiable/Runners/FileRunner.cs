using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using FAnsi.Discovery;
using Microservices.IsIdentifiable.Failures;
using Microservices.IsIdentifiable.Options;
using Microservices.IsIdentifiable.Reporting;
using NLog;
using CsvHelper;
using System.IO;
using System.Globalization;

namespace Microservices.IsIdentifiable.Runners
{
    public class FileRunner : IsIdentifiableAbstractRunner
    {
        private readonly IsIdentifiableFileOptions _opts;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public FileRunner(IsIdentifiableFileOptions opts) : base(opts)
        {
            _opts = opts;
        }

        public override int Run()
        {
            using( var fs = new StreamReader(_opts.File.OpenRead()))
            {
                var culture = string.IsNullOrWhiteSpace(_opts.Culture) ? CultureInfo.CurrentCulture : CultureInfo.GetCultureInfo(_opts.Culture);
                
                var r = new CsvReader(fs,culture);

                if(!r.Read() || !r.ReadHeader())
                    throw new Exception("Csv file had no headers");

                
                _logger.Info("Headers are:" + string.Join(",",r.Context.HeaderRecord));


                while(r.Read())
                {
                    foreach (Failure failure in GetFailuresIfAny(r))
                        AddToReports(failure);
                }
                
                CloseReports();

                return 0;
            }

        }

        private IEnumerable<Failure> GetFailuresIfAny(CsvReader r)
        {
            foreach(var h in r.Context.HeaderRecord)
            {
                var parts = new List<FailurePart>();

                parts.AddRange(Validate(h, r[h]));

                if(parts.Any())
                    yield return new Failure(parts){
                        Resource = _opts.File.FullName,
                        ResourcePrimaryKey = "Unknown",
                        ProblemValue = r[h],
                        ProblemField = h };
            }

            DoneRows(1);
        }
    }
}
