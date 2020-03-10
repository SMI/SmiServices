using System;
using System.Diagnostics;
using System.IO;
using IsIdentifiableReviewer.Out;
using Microservices.IsIdentifiable.Options;
using Microservices.IsIdentifiable.Reporting.Destinations;
using Microservices.IsIdentifiable.Reporting.Reports;

namespace IsIdentifiableReviewer
{
    public class UnattendedReviewer
    {
        private readonly Target _target;
        private readonly ReportReader _reportReader;
        private readonly RowUpdater _updater;
        private readonly IgnoreRuleGenerator _ignorer;
        private readonly FileInfo _outputFile;
         
        public int Updates = 0;
        public int Ignores = 0;
        public int Unresolved = 0;
        public int Total = 0;

        public UnattendedReviewer(IsIdentifiableReviewerOptions opts, Target target, IgnoreRuleGenerator ignorer, RowUpdater updater)
        {
            if (string.IsNullOrWhiteSpace(opts.FailuresCsv))
                throw new Exception("Unattended requires a file of errors to process");
            
            var fi = new FileInfo(opts.FailuresCsv);

            if(!fi.Exists)
                throw new FileNotFoundException($"Could not find Failures file '{fi.FullName}'");

            if(!opts.OnlyRules)
                _target = target ?? throw new Exception("A single Target must be supplied for database updates");

            _reportReader = new ReportReader(fi);

            if(string.IsNullOrWhiteSpace(opts.UnattendedOutputPath))
                throw new Exception("An output path must be specified for Failures that could not be resolved");

            _outputFile = new FileInfo(opts.UnattendedOutputPath);

            _ignorer = ignorer;
            _updater = updater;
        }

        public int Run()
        {
            //In RulesOnly mode this will be null
            var server = _target?.Discover();
            
            var storeReport = new FailureStoreReport(_outputFile.Name,100);
            
            Stopwatch sw = new Stopwatch();
            sw.Start();

            using (var storeReportDestination = new CsvDestination(new IsIdentifiableDicomFileOptions(), _outputFile))
            {
                storeReport.AddDestination(storeReportDestination);

                while(_reportReader.Next())
                {
                    //is it novel for updater
                    if(_updater.OnLoad(server,_reportReader.Current))
                        //is it novel for ignorer
                        if (_ignorer.OnLoad(_reportReader.Current,out _))
                        {
                            //we can't process it unattended
                            storeReport.Add(_reportReader.Current);
                            Unresolved++;
                        }
                        else
                            Ignores++;
                    else
                        Updates++;

                    Total++;

                    if (Total % 10000 == 0 || sw.ElapsedMilliseconds > 5000)
                    {
                        Console.WriteLine($"Done {Total:N0} u={Updates:N0} i={Ignores:N0} o={Unresolved:N0}");
                        sw.Restart();
                    }
                }

                storeReport.CloseReport();
            }
            
            Console.WriteLine($"Finished {Total:N0} updates={Updates:N0} ignored={Ignores:N0} out={Unresolved:N0}");
            return 0;
        }
    }
}