using Equ;
using Microservices.IsIdentifiable.Failures;
using Microservices.IsIdentifiable.Reporting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Microservices.CohortPackager.Execution.JobProcessing.Reporting
{
    /// <summary>
    /// Helper class to describe the information from a <see cref="Failure"/> without the associated Resource or
    /// ResourcePrimaryKey, allowing easier sorting and filtering in collections.
    /// </summary>
    public class FailureData : MemberwiseEquatable<FailureData>, IComparable<FailureData>
    {
        /// <summary>
        /// See <see cref="Failure.Parts"/>
        /// </summary>
        public ReadOnlyCollection<FailurePart> Parts { get; private set; }

        /// <summary>
        /// See <see cref="Failure.ProblemField"/>
        /// </summary>
        public string ProblemField { get; private set; }

        /// <summary>
        /// See <see cref="Failure.ProblemValue"/>
        /// </summary>
        public string ProblemValue { get; private set; }

        public FailureData(
            IEnumerable<FailurePart> parts,
            string problemField,
            string problemValue
        )
        {
            if (parts is null) throw new ArgumentNullException(nameof(parts));
            var partsList = parts.ToList();
            Parts = (partsList.Count == 0) ? throw new ArgumentException("No failure parts provided") : new ReadOnlyCollection<FailurePart>(partsList);

            ProblemField = (string.IsNullOrWhiteSpace(problemField)) ? throw new ArgumentException("Cannot be null", nameof(problemField)) : problemField;
            ProblemValue = (string.IsNullOrWhiteSpace(problemValue)) ? throw new ArgumentException("Cannot be null", nameof(problemValue)) : problemValue;
        }

        public static FailureData FromFailure(Failure f) => new(f.Parts, f.ProblemField, f.ProblemValue);

        // ^IComparable<FailureData>
        public int CompareTo(FailureData other) => Parts.Count.CompareTo(other.Parts.Count);
    }
}
