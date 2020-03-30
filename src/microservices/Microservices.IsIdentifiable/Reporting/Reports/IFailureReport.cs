
using Microservices.IsIdentifiable.Options;

namespace Microservices.IsIdentifiable.Reporting.Reports
{
    public interface IFailureReport
    {
        /// <summary>
        /// Set the destination for the report, based on the given options
        /// </summary>
        /// <param name="options"></param>
        void AddDestinations(IsIdentifiableAbstractOptions options);

        /// <summary>
        /// Call to indicate that you have processed <paramref name="numberDone"/> since you last called this method
        /// </summary>
        void DoneRows(int numberDone);

        /// <summary>
        /// Record a failure for a value on a row.  This can be called multiple times per row.  
        /// </summary>
        /// <param name="failure"></param>
        void Add(Failure failure);

        /// <summary>
        /// Finish the report and write it to the destination(s)
        /// </summary>
        void CloseReport();
    }
}