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
        private int _current = -1;
        public Failure[] Failures { get; set; }

        public int CurrentIndex => _current;
        public Failure Current => _current < Failures.Length ? Failures[_current]: null;
        public bool Exhausted => !(_current < Failures.Length);

        public ReportReader(FileInfo csvFile)
        {
            var report = new FailureStoreReport("", 0);
            Failures = report.Deserialize(csvFile).ToArray();
        }

        public bool Next()
        {
            _current++;
            if (_current < Failures.Length)
                return true;

            _current = Failures.Length;
            return false;
        }
        
        public void GoTo(int index)
        {
            _current = Math.Min(Math.Max(0,index),Failures.Length);
        }
        
        public string DescribeProgress()
        {
            return _current + "/" + Failures.Length;
        }
    }
}
