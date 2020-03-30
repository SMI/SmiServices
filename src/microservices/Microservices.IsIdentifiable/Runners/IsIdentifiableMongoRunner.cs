using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using DicomTypeTranslation;
using Microservices.IsIdentifiable.Failures;
using Microservices.IsIdentifiable.Options;
using Microservices.IsIdentifiable.Reporting;
using Microservices.IsIdentifiable.Reporting.Reports;
using MongoDB.Bson;
using MongoDB.Driver;
using NLog;
using Smi.Common.MongoDB;


namespace Microservices.IsIdentifiable.Runners
{
    public class IsIdentifiableMongoRunner : IsIdentifiableAbstractRunner
    {
        //TODO Remove this after testing
        private const bool ignorePrivateTags = true;

        private const string SEP = "#";

        private readonly ILogger _logger;

        private readonly IsIdentifiableMongoOptions _opts;

        private readonly TreeFailureReport _treeReport;

        private readonly IMongoCollection<BsonDocument> _collection;

        private readonly string _queryString;

        private readonly FindOptions<BsonDocument> _findOptionsBase = new FindOptions<BsonDocument>
        {
            NoCursorTimeout = true
        };

        private readonly ParallelOptions _parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount > 1 ? Environment.ProcessorCount / 2 : 1
        };


        private Task _runnerTask;
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();
        private bool _stopping;

        private readonly MongoDbFailureFactory _factory;


        public IsIdentifiableMongoRunner(IsIdentifiableMongoOptions opts, string appId)
            : base(opts)
        {
            _logger = LogManager.GetLogger(GetType().Name);
            _opts = opts;

            if (opts.TreeReport)
            {
                _treeReport = new TreeFailureReport(opts.GetTargetName());
                Reports.Add(_treeReport);
            }

            var mongoClient = new MongoClient(new MongoClientSettings
            {
                Server = new MongoServerAddress(_opts.HostName, _opts.Port),
                ApplicationName = appId
            });

            IMongoDatabase db = mongoClient.TryGetDatabase(_opts.DatabaseName);
            _collection = db.TryGetCollection(_opts.CollectionName);

            if (!string.IsNullOrWhiteSpace(_opts.QueryFile))
                _queryString = File.ReadAllText(_opts.QueryFile);

            // if specified, batch size must be g.t. 1:
            // https://docs.mongodb.com/manual/reference/method/cursor.batchSize/
            if (_opts.MongoDbBatchSize > 1)
                _findOptionsBase.BatchSize = _opts.MongoDbBatchSize;

            _factory = new MongoDbFailureFactory();

            if (_opts.UseMaxThreads)
                _parallelOptions.MaxDegreeOfParallelism = -1;
        }

        public override int Run()
        {
            _runnerTask = RunQuery();
            _runnerTask.Wait();

            return 0;
        }

        private async Task RunQuery()
        {
            _logger.Info("Using MaxDegreeOfParallelism: " + _parallelOptions.MaxDegreeOfParallelism);

            var totalProcessed = 0;
            var failedToRebuildCount = 0;

            _logger.Debug("Performing query");
            DateTime start = DateTime.Now;

            using (IAsyncCursor<BsonDocument> cursor = await MongoQueryParser.GetCursor(_collection, _findOptionsBase, _queryString))
            {
                _logger.Info("Query completed in {0:g}. Starting checks with cursor", (DateTime.Now - start));
                _logger.Info("Batch size is: " + (_findOptionsBase.BatchSize.HasValue ? _findOptionsBase.BatchSize.ToString() : "unspecified"));

                start = DateTime.Now;

                //Note: Can only check for the cancellation request every time we start to process a new batch
                while (await cursor.MoveNextAsync() && !_tokenSource.IsCancellationRequested)
                {
                    _logger.Debug("Received new batch");

                    IEnumerable<BsonDocument> batch = cursor.Current;
                    var batchCount = 0;

                    var batchFailures = new List<Reporting.Failure>();
                    var oListLock = new object();
                    var oLogLock = new object();

                    Parallel.ForEach(batch, _parallelOptions, document =>
                    {
                        ObjectId documentId = document["_id"].AsObjectId;
                        DicomDataset ds;

                        try
                        {
                            ds = DicomTypeTranslaterWriter.BuildDicomDataset(document);
                        }
                        catch (Exception e)
                        {
                            // Log any documents we couldn't process due to errors in rebuilding the dataset
                            lock (oLogLock)
                                _logger.Log(LogLevel.Error, e,
                                    "Could not reconstruct dataset from document " + documentId);

                            Interlocked.Increment(ref failedToRebuildCount);

                            return;
                        }

                        // Validate the dataset against our rules
                        IList<Reporting.Failure> documentFailures = ProcessDataset(documentId, ds);

                        if (documentFailures.Any())
                            lock (oListLock)
                                batchFailures.AddRange(documentFailures);

                        Interlocked.Increment(ref batchCount);
                    });

                    batchFailures.ForEach(AddToReports);

                    totalProcessed += batchCount;
                    _logger.Debug($"Processed {totalProcessed} documents total");

                    DoneRows(batchCount);
                }
            }

            TimeSpan queryTime = DateTime.Now - start;
            _logger.Info("Processing finished or cancelled, total time elapsed: " + queryTime.ToString("g"));

            _logger.Info("{0} documents were processed in total", totalProcessed);

            if (failedToRebuildCount > 0)
                _logger.Warn("{0} documents could not be reconstructed into DicomDatasets", failedToRebuildCount);

            _logger.Info("Writing out reports...");
            CloseReports();
        }

        public void Stop()
        {
            if (_stopping)
                return;

            _stopping = true;

            _logger.Info("Cancelling the running query");
            _tokenSource.Cancel();
        }


        private IList<Reporting.Failure> ProcessDataset(ObjectId documentId, DicomDataset ds, string tagTree = "")
        {
            var nodeCounts = new Dictionary<string, int>();
            var failures = new List<Reporting.Failure>();

            ds.TryGetString(DicomTag.Modality, out string modality);
            bool hasImageType = ds.TryGetValues(DicomTag.ImageType, out string[] imageTypeArr);

            var imageTypeStr = "";

            if (hasImageType)
                imageTypeStr = string.Join(@"\\", imageTypeArr.Take(2));

            // Prefix the Modality and ImageType tags to allow grouping. This is a temporary solution until the reporting API supports grouping.
            string groupPrefix = modality + SEP + imageTypeStr + SEP;

            foreach (DicomItem item in ds)
            {
                string kw = item.Tag.DictionaryEntry.Keyword;

                var asSequence = item as DicomSequence;
                if (asSequence != null)
                {
                    for (var i = 0; i < asSequence.Count(); ++i)
                    {
                        DicomDataset subDataset = asSequence.ElementAt(i);
                        string newTagTree = tagTree + kw + "[" + i + "]->";
                        failures.AddRange(ProcessDataset(documentId, subDataset, newTagTree));
                    }

                    continue;
                }

                var element = ds.GetDicomItem<DicomElement>(item.Tag);
                string fullTagPath = groupPrefix + tagTree + kw;

                //TODO OverlayRows...
                if (!nodeCounts.ContainsKey(fullTagPath))
                    nodeCounts.Add(fullTagPath, 1);
                else
                    nodeCounts[fullTagPath]++;

                if (element.Count == 0)
                    continue;

                // If it is not a (multi-)string element, continue
                if (!element.ValueRepresentation.IsString)
                    continue;

                // For each string in the element
                //TODO This is slow and should be refactored
                foreach (string s in ds.GetValues<string>(element.Tag))
                {
                    List<FailurePart> parts = Validate(kw, s).ToList();

                    if (parts.Any())
                        failures.Add(_factory.Create(documentId, fullTagPath, s, parts));
                }
            }

            AddNodeCounts(nodeCounts);

            return failures;
        }

        private void AddNodeCounts(IDictionary<string, int> nodeCounts)
        {
            if (_treeReport != null)
                _treeReport.AddNodeCounts(nodeCounts);
        }
    }
}