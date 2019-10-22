
using CommandLine;
using CommandLine.Text;
using Smi.Common.Options;
using ReusableLibraryCode.Annotations;
using System.Collections.Generic;
using System.Text;

namespace Microservices.DicomReprocessor.Options
{
    public class DicomReprocessorCliOptions : CliOptions
    {
        [Option('c', "collection-name", Required = true, HelpText = "The collection to reprocess documents from")]
        public string SourceCollection { get; set; }

        [Option('q', "query-file", Required = false, HelpText = "The file to build the reprocessing query from")]
        public string QueryFile { get; set; }

        [Option("batch-size", Required = false, HelpText = "The batch size to query MongoDB with, if specified", Default = 0)]
        public int MongoDbBatchSize { get; set; }

        [Option("sleep-time", Required = false, HelpText = "TEMP: Sleep this number of ms between batches", Default = 0)]
        public int SleepTime { get; set; }

        /// <summary>
        /// Routing key to republish messages with. Must not be null, otherwise the messages will end up back in MongoDB.
        /// Must match the routing key of the binding to the queue you wish the messages to end up in.
        /// </summary>
        [Option("reprocessing-key", Required = false, HelpText = "Routing key to reprocess messages with. Do not change from default for most cases", Default = "reprocessed")]
        public string ReprocessingRoutingKey { get; set; }

        [Option("auto-run", Required = false, HelpText = "Automatically run the query without asking for confirmation", Default = false)]
        public bool AutoRun { get; set; }


        [Usage]
        [UsedImplicitly]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return
                    new Example("Normal Scenario", new DicomReprocessorCliOptions { SourceCollection = "image", MongoDbBatchSize = 123 });
                yield return
                    new Example("Normal Scenario", new DicomReprocessorCliOptions { SourceCollection = "image", MongoDbBatchSize = 123, ReprocessingRoutingKey = "test", AutoRun = true });
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append("SourceCollection: " + SourceCollection);
            sb.Append(", QueryFile: " + QueryFile);
            sb.Append(", MongoDbBatchSize: " + MongoDbBatchSize);
            sb.Append(", ReprocessingRoutingKey: " + ReprocessingRoutingKey);
            sb.Append(", AutoRun: " + AutoRun);

            return base.ToString() + ", " + sb;
        }
    }
}
