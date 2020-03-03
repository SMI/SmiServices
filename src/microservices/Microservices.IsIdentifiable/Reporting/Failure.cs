using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microservices.IsIdentifiable.Failures;

namespace Microservices.IsIdentifiable.Reporting
{
    public class Failure
    {
        /// <summary>
        /// Each sub part of <see cref="ProblemValue"/> that the system had a problem with
        /// </summary>
        public ReadOnlyCollection<FailurePart> Parts { get; private set; }
        
        /// <summary>
        /// Description of the item being evaluated (e.g. table name, file name etc)
        /// </summary>
        public string Resource { get; set; }

        /// <summary>
        /// How to narrow down within the <see cref="Resource"/> the exact location of the <see cref="ProblemValue"/>.  Leave blank for
        /// files.  List the primary key for tables (e.g. SeriesInstanceUID / SOPInstanceUID).  Use semi colons for composite keys
        /// </summary>
        public string ResourcePrimaryKey { get; set; }

        /// <summary>
        /// The name of the column or DicomTag (including subtags if in a sequence) in which identifiable data was found
        /// </summary>
        public string ProblemField { get; set; }

        /// <summary>
        /// The full tag/column value that was identified as bad
        /// </summary>
        public string ProblemValue { get; set; }

        /// <summary>
        /// Creates a new validation failure composed of the given <paramref name="parts"/>
        /// </summary>
        /// <param name="parts"></param>
        public Failure(IEnumerable<FailurePart> parts)
        {
            Parts = new ReadOnlyCollection<FailurePart>(parts.ToList());
        }

        /// <summary>
        /// Returns true if the user looking at the <paramref name="other"/> would consider
        /// it the same as this (same problem value in same column).  Does not have to be the same
        /// origin record (Resource / Primary Key)
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool HaveSameProblem(Failure other)
        {
            if (other == null)
                return false;

            return
                string.Equals(ProblemValue, other.ProblemValue, StringComparison.CurrentCultureIgnoreCase) &&
                string.Equals(ProblemField, other.ProblemField, StringComparison.CurrentCultureIgnoreCase);
        }
    }
}
