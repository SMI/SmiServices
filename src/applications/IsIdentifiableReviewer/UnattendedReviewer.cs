using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using IsIdentifiableReviewer.Out;
using Microservices.IsIdentifiable.Options;
using Microservices.IsIdentifiable.Reporting.Destinations;
using Microservices.IsIdentifiable.Reporting.Reports;
using Microservices.IsIdentifiable.Rules;
using NLog;
using NLog.Fluent;

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
        private Logger _log;

        Dictionary<IsIdentifiableRule,int> _updateRulesUsed = new Dictionary<IsIdentifiableRule, int>();
        Dictionary<IsIdentifiableRule,int> _ignoreRulesUsed = new Dictionary<IsIdentifiableRule, int>();

        public UnattendedReviewer(IsIdentifiableReviewerOptions opts, Target target, IgnoreRuleGenerator ignorer, RowUpdater updater)
        {
            _log = LogManager.GetCurrentClassLogger();

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
            List<Exception> errors = new List<Exception>();
            
            var storeReport = new FailureStoreReport(_outputFile.Name,100);
            
            Stopwatch sw = new Stopwatch();
            sw.Start();

            using (var storeReportDestination = new CsvDestination(new IsIdentifiableDicomFileOptions(), _outputFile))
            {
                IsIdentifiableRule updateRule;

                storeReport.AddDestination(storeReportDestination);

                while(_reportReader.Next())
                {
                    bool noUpdate;
 
                    try
                    {
                        noUpdate = _updater.OnLoad(server, _reportReader.Current, out  updateRule);
                    }
                    catch (Exception e)
                    {
                        errors.Add(e);
                        continue;
                    }
                    
                    //is it novel for updater
                    if(noUpdate)
                        //is it novel for ignorer
                        if (_ignorer.OnLoad(_reportReader.Current,out IsIdentifiableRule ignoreRule))
                        {
                            //we can't process it unattended
                            storeReport.Add(_reportReader.Current);
                            Unresolved++;
                        }
                        else
                        {
                            
                            if (!_ignoreRulesUsed.ContainsKey(ignoreRule))
                                _ignoreRulesUsed.Add(ignoreRule, 1);
                            else
                                _ignoreRulesUsed[ignoreRule]++;

                            Ignores++;
                        }
                    else
                    {
                        if (!_updateRulesUsed.ContainsKey(updateRule))
                            _updateRulesUsed.Add(updateRule, 1);
                        else
                            _updateRulesUsed[updateRule]++;

                        Updates++;
                    }

                    Total++;

                    if (Total % 10000 == 0 || sw.ElapsedMilliseconds > 5000)
                    {
                        Log($"Done {Total:N0} u={Updates:N0} i={Ignores:N0} o={Unresolved:N0} err={errors.Count:N0}",true);
                        sw.Restart();
                    }
                }

                storeReport.CloseReport();
            }

            Log($"Ignore Rules Used:" + Environment.NewLine + string.Join(Environment.NewLine,
                                       _ignoreRulesUsed.OrderBy(k=>k.Value).Select(k=>$"{k.Key.IfPattern} - {k.Value:N0}")),false);

            Log($"Update Rules Used:" + Environment.NewLine + string.Join(Environment.NewLine,
                                       _updateRulesUsed.OrderBy(k=>k.Value).Select(k=>$"{k.Key.IfPattern} - {k.Value:N0}")),false);

            Log("Errors:" + Environment.NewLine + string.Join(Environment.NewLine,errors.Select(e=>e.ToString())),false);

            Log($"Finished {Total:N0} updates={Updates:N0} ignored={Ignores:N0} out={Unresolved:N0} err={errors.Count:N0}",true);
            return 0;
        }

        private void Log(string msg, bool toConsole)
        {
            _log.Info(msg);
            if(toConsole)
                Console.WriteLine(msg);
        }
    }
}