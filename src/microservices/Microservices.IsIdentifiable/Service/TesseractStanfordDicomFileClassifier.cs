using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Microservices.IsIdentifiable.Options;
using Microservices.IsIdentifiable.Reporting.Reports;
using Microservices.IsIdentifiable.Runners;

namespace Microservices.IsIdentifiable.Service
{
    public class TesseractStanfordDicomFileClassifier : Classifier, IDisposable
    {
        private DicomFileRunner _runner;

        //public TesseractStanfordDicomFileClassifier(DirectoryInfo dataDirectory) : base(dataDirectory)
        public TesseractStanfordDicomFileClassifier(DirectoryInfo dataDirectory, IsIdentifiableServiceOptions isIdentifiableServiceOptions) : base(dataDirectory)
        {
            var fileOptions = new IsIdentifiableDicomFileOptions();
            
            //need to pass this so that the runner doesn't get unhappy about there being no reports (even though we clear it below)
            fileOptions.ColumnReport = true;
            fileOptions.TessDirectory = dataDirectory.FullName;

            fileOptions.IgnoreTextLessThan = isIdentifiableServiceOptions.IgnoreTextLessThan;

            // The Rules directory is always called "IsIdentifiableRules"
            DirectoryInfo[] subDirs = dataDirectory.GetDirectories("IsIdentifiableRules");
            foreach (DirectoryInfo subDir in subDirs)
                fileOptions.RulesDirectory = subDir.FullName;

            _runner = new DicomFileRunner(fileOptions);
        }

        

        public override IEnumerable<Reporting.Failure> Classify(IFileInfo dcm)
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