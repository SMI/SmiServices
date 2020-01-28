using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microservices.IsIdentifiable.Options;
using Microservices.IsIdentifiable.Reporting.Reports;
using Microservices.IsIdentifiable.Runners;

namespace Microservices.IsIdentifiable.Service
{
    public class TesseractStanfordDicomFileClassifier : Classifier, IDisposable
    {

        public const string StanfordNerDir = "stanford-ner";
        public const string StanfordNerClassifierFilePattern = "*3class.distsim.crf.ser.gz";

        public const string TessDir = "tessdata";

        private DicomFileRunner _runner;

        public TesseractStanfordDicomFileClassifier(DirectoryInfo dataDirectory) : base(dataDirectory)
        {
            var nerDir = GetSubdirectory(StanfordNerDir);
            var nerFile = FindOneFile(StanfordNerClassifierFilePattern, nerDir);

            var fileOptions = new IsIdentifiableDicomFileOptions();
            
            //need to pass this so that the runner doesn't get unhappy about there being no reports (even though we clear it below)
            fileOptions.ColumnReport = true;
            fileOptions.PathToNerClassifier = nerFile.FullName;
            fileOptions.TessDirectory = GetSubdirectory(TessDir).FullName;


            _runner = new DicomFileRunner(fileOptions);
        }

        

        public override IEnumerable<Reporting.Failure> Classify(FileInfo dcm)
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