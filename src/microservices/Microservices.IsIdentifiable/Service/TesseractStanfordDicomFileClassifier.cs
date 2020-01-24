using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microservices.IsIdentifiable.Failure;
using Microservices.IsIdentifiable.Options;
using Microservices.IsIdentifiable.Reporting.Reports;
using Microservices.IsIdentifiable.Runners;

namespace Microservices.IsIdentifiable.Service
{
    public class TesseractStanfordDicomFileClassifier : Classifier
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
            fileOptions.PathToNerClassifier = nerFile.FullName;
            fileOptions.TessDirectory = GetSubdirectory(TessDir).FullName;


            _runner = new DicomFileRunner(fileOptions);
        }

        

        public override IEnumerable<FailurePart> Classify(FileInfo dcm)
        {
            throw new NotImplementedException();
        }

    }
}