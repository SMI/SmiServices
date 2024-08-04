
using CommandLine;
using CommandLine.Text;
using SmiServices.Common.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmiServices.Microservices.DicomReprocessor
{
    public class DicomReprocessorCliOptions : CliOptions
    {
        private string _sourceCollection = null!;
        [Option(
            'c',
            "collection-name",
            Required = true,
            HelpText = "The collection to reprocess documents from. This is the collection (table) only not the database which is determined by \"MongoDatabases.DicomStoreOptions\" in the YAML config"
        )]
        public string SourceCollection
        {
            get => _sourceCollection;
            set
            {
                if (value.Contains("."))
                    throw new ArgumentException(nameof(value));
                _sourceCollection = value;
            }
        }

        [Option(
            'q',
            "query-file",
            Required = false,
            HelpText = "[Optional] The file to build the reprocessing query from (if you only want a subset of the collection)"
        )]
        public string? QueryFile { get; set; }

        [Option(
            "batch-size",
            Default = 0,
            Required = false,
            HelpText = "[Optional] The batch size to set for queries executed on MongoDB. If not set, MongoDB will adjust the batch size automatically"
        )]
        public int MongoDbBatchSize { get; set; }

        [Option(
            "sleep-time-ms",
            Default = 0,
            Required = false,
            HelpText = "[Optional] Sleep this number of ms between batches"
        )]
        public int SleepTimeMs { get; set; }


        /// <summary>
        /// Routing key to republish messages with. Must not be null, otherwise the messages will end up back in MongoDB.
        /// Must match the routing key of the binding to the queue you wish the messages to end up in.
        /// </summary>
        [Option(
            "reprocessing-key",
            Default = "reprocessed",
            Required = false,
            HelpText = "[Optional] Routing key for output messages sent to the RabbitMQ exchange, which may depend on your RabbitMQ configuration. The exchange must have a valid route mapped for this routing key"
        )]
        public string? ReprocessingRoutingKey { get; set; }

        [Option(
            "auto-run",
            Default = false,
            Required = false,
            HelpText = "[Optional] False (default) waits for user confirmation that the query is correct before continuing"
        )]
        public bool AutoRun { get; set; }


        [Usage]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return
                    new Example("Normal Scenario", new DicomReprocessorCliOptions { SourceCollection = "image_CT" });
                yield return
                    new Example("Unattended with non-default parameters", new DicomReprocessorCliOptions
                    {
                        SourceCollection = "image_CT",
                        QueryFile = "test",
                        MongoDbBatchSize = 123,
                        SleepTimeMs = 1000,
                        ReprocessingRoutingKey = "test",
                        AutoRun = true
                    });
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
