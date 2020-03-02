using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microservices.IsIdentifiable.Reporting;
using Microservices.IsIdentifiable.Reporting.Reports;

namespace IsIdentifiableReviewer
{
    class ReportReader
    {
        private int _current;
        public Failure[] Failures { get; set; }

        public Failure Current => _current < Failures.Length ? Failures[_current]: null;

        public ReportReader(FileInfo csvFile)
        {
            var report = new FailureStoreReport("", 0);
            Failures = report.Deserialize(csvFile).ToArray();
        }

        public bool Next()
        {
            _current++;
            return _current < Failures.Length;
        }

        public void GoTo(int index)
        {
            _current = index;
        }
    }
}
