using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using IsIdentifiable.Options;
using IsIdentifiable.Reporting.Reports;
using IsIdentifiable.Runners;
using IsIdentifiable.Reporting;

namespace Microservices.IsIdentifiable.Service
{
    public class TesseractStanfordDicomFileClassifier : Classifier, IDisposable
    {
        private DicomFileRunner _runner;

        public TesseractStanfordDicomFileClassifier(IDirectoryInfo dataDirectory, IsIdentifiableDicomFileOptions fileOptions) : base(dataDirectory)
        {            
            //need to pass this so that the runner doesn't get unhappy about there being no reports (even though we clear it below)
            fileOptions.ColumnReport = true;
            fileOptions.TessDirectory = dataDirectory.FullName;

            // The Rules directory is always called "IsIdentifiableRules"
            var subDirs = dataDirectory.GetDirectories("IsIdentifiableRules");
            foreach (var subDir in subDirs)
                fileOptions.RulesDirectory = subDir.FullName;

            _runner = new DicomFileRunner(fileOptions, dataDirectory.FileSystem);
        }

        

        public override IEnumerable<Failure> Classify(IFileInfo dcm)
        {
            _runner.Reports.Clear();
            var toMemory = new ToMemoryFailureReport();
            _runner.Reports.Add(toMemory);
            _runner.ValidateDicomFile(dcm);

            return toMemory.Failures;
        }

        public void Dispose()
        {
            _runner?.Dispose();
        }
    }
}
