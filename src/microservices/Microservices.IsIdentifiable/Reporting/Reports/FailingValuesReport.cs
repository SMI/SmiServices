using System;
using System.Collections.Generic;
using System.Data;

namespace Microservices.IsIdentifiable.Reporting.Reports
{
    internal class FailingValuesReport : FailureReport
    {
        private readonly object _oFailuresLock = new object();
        private readonly Dictionary<string, HashSet<string>> _failures = new Dictionary<string, HashSet<string>>();

        public FailingValuesReport(string targetName)
            : base(targetName) { }

        public override void Add(Failure failure)
        {
            lock (_oFailuresLock)
            {
                if (!_failures.ContainsKey(failure.ProblemField))
                    _failures.Add(failure.ProblemField, new HashSet<string>(StringComparer.CurrentCultureIgnoreCase));

                _failures[failure.ProblemField].Add(failure.ProblemValue);
            }
        }

        protected override void CloseReportBase()
        {
            var dt = new DataTable();
            dt.Columns.Add("Field");
            dt.Columns.Add("Value");


            lock (_oFailuresLock)
                foreach (KeyValuePair<string, HashSet<string>> kvp in _failures)
                    foreach (string v in kvp.Value)
                        dt.Rows.Add(kvp.Key, v);

            Destinations.ForEach(d => d.WriteItems(dt));
        }
    }
}
