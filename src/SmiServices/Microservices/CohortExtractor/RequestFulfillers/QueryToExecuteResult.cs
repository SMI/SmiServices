using Equ;
using System;


namespace SmiServices.Microservices.CohortExtractor.RequestFulfillers
{
    public class QueryToExecuteResult : MemberwiseEquatable<QueryToExecuteResult>
    {
        public readonly string FilePathValue;
        public readonly string? StudyTagValue;
        public readonly string? SeriesTagValue;
        public readonly string? InstanceTagValue;

        public readonly bool Reject;
        public readonly string? RejectReason;

        public QueryToExecuteResult(
            string filePathValue,
            string? studyTagValue,
            string? seriesTagValue,
            string? instanceTagValue,
            bool rejection,
            string? rejectionReason
        )
        {
            FilePathValue = filePathValue;
            StudyTagValue = studyTagValue;
            SeriesTagValue = seriesTagValue;
            InstanceTagValue = instanceTagValue;
            Reject = rejection;
            RejectReason = rejectionReason;

            if (Reject && string.IsNullOrWhiteSpace(RejectReason))
                throw new ArgumentException("RejectReason must be specified if Reject=true");
        }

        public override string ToString()
        {
            return $"{FilePathValue}(Reject={Reject})";
        }
    }
}
