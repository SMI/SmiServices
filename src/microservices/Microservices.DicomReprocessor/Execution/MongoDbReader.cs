
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microservices.DicomReprocessor.Execution.Processors;
using Microservices.DicomReprocessor.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using NLog;
using Smi.Common.MongoDB;
using Smi.Common.Options;


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

        private readonly FindOptions<BsonDocument> _findOptionsBase = new()
        {
            NoCursorTimeout = true
        };

        private readonly ParallelOptions _parallelOptions = new()
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount > 1 ? Environment.ProcessorCount / 2 : 1
        };

        private readonly CancellationTokenSource _tokenSource = new();
        private bool _stopping;

        private readonly bool _autoRun;

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

            _autoRun = reprocessorOptions.AutoRun;
        }


        public async Task<TimeSpan> RunQuery(string? query, IDocumentProcessor processor, DicomReprocessorOptions options)
        {
            DateTime start;

            _logger.Info($"Performing query on {_collNamespace}");

            using (IAsyncCursor<BsonDocument> cursor = await MongoQueryParser.GetCursor(_collection, _findOptionsBase, query))
            {
                _logger.Info($"Using MaxDegreeOfParallelism: {_parallelOptions.MaxDegreeOfParallelism}");
                _logger.Info($"Batch size is: {(_findOptionsBase.BatchSize.HasValue ? _findOptionsBase.BatchSize.ToString() : "unspecified")}");
                _logger.Info($"Sleeping for {options.SleepTime.TotalMilliseconds}ms between batches");

                if (!_autoRun)
                {
                    LogManager.Flush();

                    Console.Write($"Confirm you want to reprocess documents using the above query in {_collNamespace} [y/N]: ");

                    // Anything other than y/Y cancels the operation
                    string? key = Console.ReadLine();
                    if (key == null || !key.Equals("y", StringComparison.CurrentCultureIgnoreCase))
                    {
                        _logger.Warn("User cancelled reprocessing by not answering 'y', exiting");
                        return default(TimeSpan);
                    }

                    _logger.Info("User chose to continue with query execution");
                }

                _logger.Info("Starting reprocess operation");
                start = DateTime.Now;
                var totalBatches = 0;

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

                    if (++totalBatches % 100 == 0)
                        processor.LogProgress();

                    _logger.Debug($"Batch processed, sleeping for {options.SleepTime.TotalMilliseconds}ms");
                    Thread.Sleep(options.SleepTime);
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
