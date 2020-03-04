using System;
using System.IO;
using IsIdentifiableReviewer.Out;
using Microservices.IsIdentifiable.Options;
using Microservices.IsIdentifiable.Reporting.Destinations;
using Microservices.IsIdentifiable.Reporting.Reports;

namespace IsIdentifiableReviewer
{
    internal class UnattendedReviewer
    {
        private readonly Target _target;
        private readonly ReportReader _reportReader;
        private readonly RowUpdater _updater;
        private readonly IgnoreRuleGenerator _ignorer;
        private readonly FileInfo _outputFile;

        public UnattendedReviewer(IsIdentifiableReviewerOptions opts, Target target)
        {
            if (string.IsNullOrWhiteSpace(opts.FailuresCsv))
                throw new Exception("Unattended requires a file of errors to process");
            _target = target;

            var fi = new FileInfo(opts.FailuresCsv);

            if(!fi.Exists)
                throw new FileNotFoundException($"Could not find Failures file '{fi.FullName}'");

            _outputFile = fi;

            _reportReader = new ReportReader(fi);
            _updater = new RowUpdater();
            _ignorer = new IgnoreRuleGenerator();
        }

        public int Run()
        {
            int updates = 0;
            int ignores = 0;
            int unresolved = 0;
            int total = 0;

            var server = _target.Discover();
            
            var storeReport = new FailureStoreReport(_outputFile.Name,100);
            
            using (var storeReportDestination = new CsvDestination(new IsIdentifiableDicomFileOptions(), _outputFile))
            {
                storeReport.AddDestination(storeReportDestination);

                //todo does this skip element 0?
                while(_reportReader.Next())
                {
                    //is it novel for updater
                    if(_updater.OnLoad(server,_reportReader.Current))
                        //is it novel for ignorer
                        if (_ignorer.OnLoad(_reportReader.Current))
                        {
                            //we can't process it unattended
                            storeReport.Add(_reportReader.Current);
                            unresolved++;
                        }
                        else
                            ignores++;
                    else
                        updates++;

                    total++;

                    if(total% 100 == 0)
                        Console.WriteLine($"Done {total:N0} u={updates:N0} i={ignores:N0} o={unresolved:N0}");
                }
            }
            
            Console.WriteLine($"Finished {total:N0} u={updates:N0} i={ignores:N0} o={unresolved:N0}");
            return 0;
        }
    }
}