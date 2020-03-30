using System;
using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace Microservices.IsIdentifiable.Options
{
    [Verb("mongo")]
    public class IsIdentifiableMongoOptions : IsIdentifiableDicomOptions
    {
        [Option('h', "mongo-host", Required = false, HelpText = "The MongoDB hostname or IP", Default = "localhost")]
        public string HostName { get; set; }

        [Option('p', "mongo-port", Required = false, HelpText = "The MongoDB port", Default = 27017)]
        public int Port { get; set; }

        [Option('d',"db", Required = true, HelpText = "The database to scan")]
        public string DatabaseName { get; set; }

        [Option('l',"coll", Required = true, HelpText = "The collection to scan")]
        public string CollectionName { get; set; }

        [Option('q', "query-file", Required = false, HelpText = "The file to build the reprocessing query from")]
        public string QueryFile { get; set; }

        [Option("batch-size", Required = false, HelpText = "The batch size to query MongoDB with, if specified", Default = 0)]
        public int MongoDbBatchSize { get; set; }

        [Option(HelpText = "If set will use the max. number of threads available, otherwise defaults to half the available threads")]
        public bool UseMaxThreads { get; set; }

        [Usage]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Run on a MongoDB database", new IsIdentifiableMongoOptions
                {
                    HostName = "localhost",
                    Port = 1234,
                    DatabaseName = "dicom",
                    CollectionName = "images",
                    MongoDbBatchSize = 5000
                });
            }
        }

        public override string GetTargetName()
        {
            return "MongoDB-" + DatabaseName + "-" + CollectionName + "-";
        }

        public override void ValidateOptions()
        {
            base.ValidateOptions();

            if (ColumnReport)
                throw new Exception("ColumnReport can't be generated from a MongoDB source");
        }
    }
}
