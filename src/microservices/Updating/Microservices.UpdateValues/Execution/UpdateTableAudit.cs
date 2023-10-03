using FAnsi.Discovery;
using System.Diagnostics;
using System.Threading;

namespace Microservices.UpdateValues.Execution
{
    public class UpdateTableAudit
    {

        /// <summary>
        /// The number of update queries that have been sent to the table so far
        /// </summary>
        public int Queries;

        /// <summary>
        /// The total amount of affected rows returned from the DBMS across all queries sent
        /// </summary>

        public int AffectedRows;

        /// <summary>
        /// The total length of time spent running queries on this <see cref="Table"/>
        /// </summary>
        public Stopwatch Stopwatch {get;} = new Stopwatch();

        /// <summary>
        /// The number of queries currently executing
        /// </summary>
        public int ExecutingQueries = 0;

        /// <summary>
        /// Lock for <see cref="Stopwatch"/>
        /// </summary>
        private object lockWatch = new();

        /// <summary>
        /// The table that is being updated
        /// </summary>
        public DiscoveredTable? Table { get; }

        public UpdateTableAudit(DiscoveredTable? t)
        {
            Table = t;
        }


        public void StartOne()
        {
            Interlocked.Increment(ref ExecutingQueries);
            Interlocked.Increment(ref Queries);
            
            Stopwatch.Start();
        }

        public void EndOne(int affectedRows)
        {
            Interlocked.Add(ref AffectedRows,affectedRows);
            Interlocked.Decrement(ref ExecutingQueries);
            
            lock(lockWatch)
            {
                if(ExecutingQueries == 0)
                {
                    Stopwatch.Stop();
                }
            }
        }

        public override string ToString()
        {
            return $"Table:{Table?.GetFullyQualifiedName()} Queries:{Queries} Time:{Stopwatch.Elapsed:c} AffectedRows:{AffectedRows:N0} ExecutingQueries:{ExecutingQueries}";
        }
    }
}
