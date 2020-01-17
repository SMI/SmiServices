using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;

namespace Microservices.IsIdentifiable.Reporting.Reports
{
    internal class ColumnFailureReport : FailureReport
    {
        private int _rowsProcessed;

        private readonly object _oFailureCountLock = new object();
        private readonly Dictionary<string, int> _failureCounts = new Dictionary<string, int>();


        public ColumnFailureReport(string targetName)
            : base(targetName) { }

        public override void DoneRows(int numberDone)
        {
            Interlocked.Add(ref _rowsProcessed, numberDone);
        }

        public override void Add(Failure failure)
        {
            lock (_oFailureCountLock)
            {
                if (!_failureCounts.ContainsKey(failure.ProblemField))
                    _failureCounts.Add(failure.ProblemField, 0);

                _failureCounts[failure.ProblemField]++;
            }
        }

        protected override void CloseReportBase()
        {
            if (_rowsProcessed == 0)
                throw new Exception("No rows were processed");

            var dt = new DataTable();

            lock (_oFailureCountLock)
            {
                foreach (string col in _failureCounts.Keys)
                    dt.Columns.Add(col);

                DataRow r = dt.Rows.Add();

                foreach (KeyValuePair<string, int> kvp in _failureCounts)
                    r[kvp.Key] = ((double)kvp.Value) / _rowsProcessed;
            }

            Destinations.ForEach(d => d.WriteItems(dt));
        }
    }
}
