using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using Microservices.IsIdentifiable.Options;

namespace Microservices.IsIdentifiable.Reporting.Reports
{
    /// <summary>
    /// Failure report for items from tree-like data such as MongoDB documents.
    /// </summary>
    internal class TreeFailureReport : FailureReport
    {
        private const int TOTAL_SEEN_IDX = 0;
        private const int TOTAL_FAILED_IDX = 1;

        private readonly string[] _headerRow = { "Node", "TotalSeen", "TotalFailed", "PercentFailed" };

        private readonly Regex _nodeRegex = new Regex(@"\[\d+]->");

        private readonly object _nodeFailuresLock = new object();

        // <NodePath, [TotalSeen, TotalFailed]>
        private readonly SortedDictionary<string, int[]> _nodeFailures = new SortedDictionary<string, int[]>();

        private readonly bool _reportAggregateCounts;

        public TreeFailureReport(string targetName, bool reportAggregateCounts = false)
            : base(targetName)
        {
            _reportAggregateCounts = reportAggregateCounts;
        }

        public override void AddDestinations(IsIdentifiableAbstractOptions opts)
        {
            base.AddDestinations(opts);
            Destinations.ForEach(d => d.WriteHeader(_headerRow));
        }

        public override void Add(Failure failure)
        {
            IncrementCounts(failure.ProblemField, TOTAL_FAILED_IDX);
        }

        /// <summary>
        /// Record seen nodes and their counts
        /// </summary>
        /// <param name="nodeCounts"></param>
        public void AddNodeCounts(IDictionary<string, int> nodeCounts)
        {
            foreach (KeyValuePair<string, int> kvp in nodeCounts)
                IncrementCounts(kvp.Key, TOTAL_SEEN_IDX, kvp.Value);
        }

        protected override void CloseReportBase()
        {
            using(var dt = new DataTable())
            {
                foreach (string col in _headerRow)
                    dt.Columns.Add(col);

                if (_reportAggregateCounts)
                    GenerateAggregateCounts();

                lock (_nodeFailuresLock)
                    foreach (KeyValuePair<string, int[]> item in _nodeFailures.Where(f => f.Value[TOTAL_SEEN_IDX] != 0))
                    {
                        int seen = item.Value[TOTAL_SEEN_IDX];
                        int failed = item.Value[TOTAL_FAILED_IDX];

                        dt.Rows.Add(item.Key, seen, failed, 100.0 * failed / seen);
                    }

                foreach(var d in Destinations)
                    d.WriteItems(dt);
            }

            
        }

        private void IncrementCounts(string key, int index, int count = 1)
        {
            lock (_nodeFailuresLock)
            {
                if (!_nodeFailures.ContainsKey(key))
                    _nodeFailures.Add(key, new[] { 0, 0 });

                _nodeFailures[key][index] += count;
            }
        }

        private void GenerateAggregateCounts()
        {
            lock (_nodeFailuresLock)
            {
                foreach (KeyValuePair<string, int[]> failureInfo in _nodeFailures)
                {
                    if (!_nodeRegex.IsMatch(failureInfo.Key))
                        continue;

                    //TODO Magic
                }
            }
        }
    }
}
