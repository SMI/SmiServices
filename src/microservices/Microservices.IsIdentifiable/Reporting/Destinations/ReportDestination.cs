using System.Data;
using System.Text.RegularExpressions;
using Microservices.IsIdentifiable.Options;

namespace Microservices.IsIdentifiable.Reporting.Destinations
{
    public abstract class ReportDestination : IReportDestination
    {
        protected IsIdentifiableAbstractOptions Options { get; }

        private readonly Regex _multiSpaceRegex = new Regex(" {2,}");


        protected ReportDestination(IsIdentifiableAbstractOptions options)
        {
            Options = options;
        }

        public virtual void WriteHeader(params string[] headers) { }

        public abstract void WriteItems(DataTable batch);

        public virtual void Dispose() { }

        /// <summary>
        /// Returns o with whitespace stripped (if it is a string and <see cref="IsIdentifiableAbstractOptions.DestinationNoWhitespace"/> is set on command line options).
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        protected object StripWhitespace(object o)
        {
            var s = o as string;

            if (s != null && Options.DestinationNoWhitespace)
                return _multiSpaceRegex.Replace(s.Replace("\t", "").Replace("\r", "").Replace("\n", ""), " ");

            return o;
        }
    }
}