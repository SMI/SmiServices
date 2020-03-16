using System;
using System.IO;
using JetBrains.Annotations;
using Microservices.CohortPackager.Execution.ExtractJobStorage;


namespace Microservices.CohortPackager.Execution.JobProcessing.Reporting
{
    [UsedImplicitly]
    public class FileReporter : JobReporterBase
    {
        private readonly string _reportDir;

        private FileStream _fileStream;


        public FileReporter(
            [NotNull] IExtractJobStore jobStore,
            [NotNull] string reportDir
        )
            : base(jobStore)
        {
            _reportDir = reportDir ?? throw new ArgumentNullException(nameof(reportDir));
        }

        protected override Stream GetStream(Guid jobId)
        {
            string jobReport = $"{_reportDir}/{jobId}.txt";
            if (File.Exists(jobReport))
                throw new ApplicationException($"Report file '{jobReport}' already exists");

            _fileStream = File.OpenWrite(jobReport);
            return _fileStream;
        }

        protected override void FinishReport(Stream stream) { }

        protected override void ReleaseUnmanagedResources()
        {
            if (_fileStream != null)
                _fileStream.Dispose();
        }

        public override void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~FileReporter() => ReleaseUnmanagedResources();
    }
}