
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
        [Option('c', "collection-name", Required = true, HelpText = "The collection to reprocess documents from.  This is the collection (table) only not the database which is determined by the yaml file settings MongoDatabases.DicomStoreOptions")]
        public string SourceCollection { get; set; }

        [Option('q', "query-file", Required = false, HelpText = "Optional - The file to build the reprocessing query from (if you only want a subset of the collection)")]
        public string QueryFile { get; set; }

        [Option("batch-size", Required = false, HelpText = "Optional - The batch size to set for queries executed on MongoDB", Default = 0)]
        public int MongoDbBatchSize { get; set; }

        [Option("sleep-time", Required = false, HelpText = "TEMP: Sleep this number of ms between batches", Default = 0)]
        public int SleepTime { get; set; }

        /// <summary>
        /// Routing key to republish messages with. Must not be null, otherwise the messages will end up back in MongoDB.
        /// Must match the routing key of the binding to the queue you wish the messages to end up in.
        /// </summary>
        [Option("reprocessing-key", Required = false, HelpText = "Routing key for output messages sent to the RabbitMQ exchange (see default.yaml).  The exchange must have a valid route mapped for this routing key.", Default = "reprocessed")]
        public string ReprocessingRoutingKey { get; set; }

        [Option("auto-run", Required = false, HelpText = "False (default) for interactive mode, True for automatic (unattended) execution", Default = false)]
        public bool AutoRun { get; set; }


        [Usage]
        [UsedImplicitly]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return
                    new Example("Normal Scenario", new DicomReprocessorCliOptions { SourceCollection = "image_CT" });
                yield return
                    new Example("Unattended with custom routing key / batch size", new DicomReprocessorCliOptions { SourceCollection = "image_CT", MongoDbBatchSize = 123, ReprocessingRoutingKey = "test", AutoRun = true });
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
