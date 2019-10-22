
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Smi.Common.Options;
using Microservices.DicomReprocessor.Execution.Processors;
using Microservices.DicomReprocessor.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using NLog;
using Smi.MongoDB.Common;

namespace Microservices.DicomReprocessor.Execution
{
    public class MongoDbReader
    {
        public bool WasCancelled
        {
            get { return _tokenSource.IsCancellationRequested; }
        }


        private readonly ILogger _logger;

        private readonly string _collNamespace;
        private readonly IMongoCollection<BsonDocument> _collection;

        private readonly FindOptions<BsonDocument> _findOptionsBase = new FindOptions<BsonDocument>
        {
            NoCursorTimeout = true
        };

        private readonly ParallelOptions _parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount > 1 ? Environment.ProcessorCount / 2 : 1
        };

        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();
        private bool _stopping;


        public MongoDbReader(MongoDbOptions mongoOptions, DicomReprocessorCliOptions reprocessorOptions, string appId)
        {
            _logger = LogManager.GetLogger(GetType().Name);

            MongoClient mongoClient = MongoClientHelpers.GetMongoClient(mongoOptions, appId);

            if (string.IsNullOrWhiteSpace(reprocessorOptions.SourceCollection))
                throw new ArgumentException("SourceCollection");

            _collection = mongoClient.GetDatabase(mongoOptions.DatabaseName).GetCollection<BsonDocument>(reprocessorOptions.SourceCollection);
            _collNamespace = mongoOptions.DatabaseName + "." + reprocessorOptions.SourceCollection;

            // if specified, batch size must be gt 1:
            // https://docs.mongodb.com/manual/reference/method/cursor.batchSize/
            if (reprocessorOptions.MongoDbBatchSize > 1)
                _findOptionsBase.BatchSize = reprocessorOptions.MongoDbBatchSize;
        }


        public async Task<TimeSpan> RunQuery(string query, IDocumentProcessor processor, int sleepDuration, bool autoRun = false)
        {
            DateTime start;

            _logger.Debug("Performing query");

            using (IAsyncCursor<BsonDocument> cursor = await MongoQueryParser.GetCursor(_collection, _findOptionsBase, query))
            {
                _logger.Info("Using MaxDegreeOfParallelism: " + _parallelOptions.MaxDegreeOfParallelism);
                _logger.Info("Batch size is: " + (_findOptionsBase.BatchSize.HasValue ? _findOptionsBase.BatchSize.ToString() : "unspecified"));
                _logger.Info("Sleeping for " + sleepDuration + "ms between batches");

                if (!autoRun)
                {
                    LogManager.Flush();

                    Console.Write("Confirm you want to reprocess documents using the above query in " + _collNamespace + ": ");

                    string key = Console.ReadLine();
                    if (key == null || key.ToLower() != "y")
                    {
                        _logger.Warn("Reprocessing cancelled, exiting");
                        return default;
                    }
                }

                _logger.Info("Starting reprocess operation");
                start = DateTime.Now;

                //Note: Can only check for the cancellation request every time we start to process a new batch
                while (await cursor.MoveNextAsync() && !_tokenSource.IsCancellationRequested)
                {
                    _logger.Debug("Received new batch");

                    IEnumerable<BsonDocument> batch = cursor.Current;
                    var batchCount = 0;

                    Parallel.ForEach(batch, _parallelOptions, document =>
                    {
                        processor.ProcessDocument(document);

                        Interlocked.Increment(ref batchCount);
                    });

                    _logger.Debug("Batch converted to messages, count was: " + batchCount);

                    processor.SendMessages();

                    _logger.Debug("Batch processed, sleeping for " + sleepDuration + "ms");
                    Thread.Sleep(sleepDuration);
                }
            }

            TimeSpan queryTime = DateTime.Now - start;
            _logger.Info("Reprocessing finished or cancelled, time elapsed: " + queryTime.ToString("g"));

            return queryTime;
        }

        public void Stop()
        {
            if (_stopping)
                return;

            _stopping = true;

            _logger.Info("Cancelling the running query");
            _tokenSource.Cancel();
        }
    }
}
