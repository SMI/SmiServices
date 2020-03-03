using System.Collections.Generic;
using Microservices.IsIdentifiable.Options;

namespace Microservices.IsIdentifiable.Reporting.Reports
{
    public class ToMemoryFailureReport : IFailureReport
    {
        public List<Failure> Failures { get; } = new List<Reporting.Failure>();


        public void AddDestinations(IsIdentifiableAbstractOptions options)
        {
            
        }

        public void DoneRows(int numberDone)
        {
            
        }

        public void Add(Reporting.Failure failure)
        {
            Failures.Add(failure);
        }

        public void CloseReport()
        {
            
        }
    }
}