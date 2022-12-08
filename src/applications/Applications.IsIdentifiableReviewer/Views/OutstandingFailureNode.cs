using Microservices.IsIdentifiable.Failures;
using Microservices.IsIdentifiable.Reporting;
using Terminal.Gui;
using Terminal.Gui.Trees;

namespace IsIdentifiableReviewer.Views
{
    internal class OutstandingFailureNode : TreeNode
    {
        /// <summary>
        /// The first failure that was seen after which all <see cref="NumberOfTimesReported"/> only needs to match the <see cref="Failure.ProblemValue"/> (i.e. not the offset or the classification)
        /// </summary>
        public Failure Failure{ get; }

        /// <summary>
        /// Number of times the <see cref="FailurePart.Word"/> was seen in the report being evaluated
        /// </summary>
        public int NumberOfTimesReported;

        public OutstandingFailureNode(Failure failure, int numberOfTimesReported)
        {
            Failure = failure;
            NumberOfTimesReported = numberOfTimesReported;
        }

        public override string ToString()
        {
            return $"{ Failure.ProblemValue} x{NumberOfTimesReported:N0}";
        }
    }
}
