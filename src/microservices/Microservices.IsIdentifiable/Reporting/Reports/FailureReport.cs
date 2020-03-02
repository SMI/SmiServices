using System;
using System.Collections.Generic;
using Microservices.IsIdentifiable.Options;
using Microservices.IsIdentifiable.Reporting.Destinations;

namespace Microservices.IsIdentifiable.Reporting.Reports
{
    public abstract class FailureReport : IFailureReport
    {
        private readonly string _reportName;

        public List<IReportDestination> Destinations = new List<IReportDestination>();


        /// <summary>
        /// Creates a new report aimed at the given resource (e.g. "MR_ImageTable")
        /// </summary>
        /// <param name="targetName"></param>
        protected FailureReport(string targetName)
        {
            _reportName = targetName + GetType().Name;
        }

        /// <summary>
        /// Creates report destinations. Can be overriden to add headers or to initialize the destination in some way.
        /// </summary>
        /// <param name="opts"></param>
        public virtual void AddDestinations(IsIdentifiableAbstractOptions opts)
        {
            IReportDestination destination;

            // Default is to write out CSV results
            if (!string.IsNullOrWhiteSpace(opts.DestinationCsvFolder))
                destination = new CsvDestination(opts, _reportName);
            else if (!string.IsNullOrWhiteSpace(opts.DestinationConnectionString))
                destination = new DatabaseDestination(opts, _reportName);
            else
            {
                opts.DestinationCsvFolder = Environment.CurrentDirectory;
                destination = new CsvDestination(opts, _reportName);
            }

            Destinations.Add(destination);
        }

        public virtual void DoneRows(int numberDone) { }

        public abstract void Add(Failure failure);

        public void CloseReport()
        {
            CloseReportBase();
            Destinations.ForEach(d => d.Dispose());
        }

        /// <summary>
        /// Writes out (the rest of) the report before exiting
        /// </summary>
        protected abstract void CloseReportBase();
    }
}