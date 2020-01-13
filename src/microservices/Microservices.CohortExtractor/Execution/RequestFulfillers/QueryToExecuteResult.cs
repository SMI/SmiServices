using System;

namespace Microservices.CohortExtractor.Execution.RequestFulfillers
{
    public class QueryToExecuteResult : IEquatable<QueryToExecuteResult>
    {
        public readonly string FilePathValue;
        public readonly string StudyTagValue;
        public readonly string SeriesTagValue;
        public readonly string InstanceTagValue;
        
        #region Equality
        public QueryToExecuteResult(string filePathValue, string studyTagValue, string seriesTagValue, string instanceTagValue)
        {
            FilePathValue = filePathValue;
            StudyTagValue = studyTagValue;
            SeriesTagValue = seriesTagValue;
            InstanceTagValue = instanceTagValue;
        }

        public bool Equals(QueryToExecuteResult other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return FilePathValue == other.FilePathValue && StudyTagValue == other.StudyTagValue && SeriesTagValue == other.SeriesTagValue && InstanceTagValue == other.InstanceTagValue;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((QueryToExecuteResult) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (FilePathValue != null ? FilePathValue.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (StudyTagValue != null ? StudyTagValue.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (SeriesTagValue != null ? SeriesTagValue.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (InstanceTagValue != null ? InstanceTagValue.GetHashCode() : 0);
                return hashCode;
            }
        }

        
        #endregion
    }
}