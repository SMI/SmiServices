
using DicomTypeTranslation;
using FellowOakDicom;
using MongoDB.Bson;
using SmiServices.Common.Messages;
using SmiServices.Common.MongoDB;
using SmiServices.Common.Options;
using System;
using System.Collections.Generic;
using System.Linq;


namespace SmiServices.Microservices.MongoDBPopulator.Processing
{
    /// <summary>
    /// Delegate class used to perform the actual processing of messages
    /// </summary>
    public class SeriesMessageProcessor : MessageProcessor<SeriesMessage>
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="options"></param>
        /// <param name="mongoDbAdapter"></param>
        /// <param name="maxQueueSize"></param>
        /// <param name="exceptionCallback"></param>
        public SeriesMessageProcessor(MongoDbPopulatorOptions options, IMongoDbAdapter mongoDbAdapter, int maxQueueSize, Action<Exception> exceptionCallback)
            : base(options, mongoDbAdapter, maxQueueSize, exceptionCallback) { }

        public override void AddToWriteQueue(SeriesMessage message, IMessageHeader header, ulong deliveryTag)
        {
            // Only time we are not processing is if we are shutting down anyway
            if (IsStopping)
                return;

            if (Model == null)
                throw new ApplicationException("Model needs to be set before messages can be processed");

            DicomDataset dataset;

            try
            {
                dataset = DicomTypeTranslater.DeserializeJsonToDataset(message.DicomDataset);

            }
            catch (Exception e)
            {
                throw new ApplicationException("Could not deserialize json to dataset", e);
            }

            BsonDocument datasetDoc;

            try
            {
                datasetDoc = DicomTypeTranslaterReader.BuildBsonDocument(dataset);
            }
            catch (Exception e)
            {
                throw new ApplicationException("Exception converting dataset to BsonDocument", e);
            }

            BsonDocument bsonHeader = MongoDocumentHeaders.SeriesDocumentHeader(message);

            BsonDocument document = new BsonDocument()
                                .Add("header", bsonHeader)
                                .AddRange(datasetDoc);

            int docByteLength = document.ToBson().Length;
            if (docByteLength > MaxDocumentSize)
                throw new ApplicationException($"BsonDocument was larger than the max allowed size (have {docByteLength}, max is {MaxDocumentSize})");

            var forceProcess = false;

            lock (LockObj)
            {
                ToProcess.Enqueue(new Tuple<BsonDocument, IMessageHeader, ulong>(document, header, deliveryTag));

                if (ToProcess.Count >= MaxQueueSize)
                    forceProcess = true;
            }

            if (!forceProcess)
                return;

            Logger.Debug("SeriesMessageProcessor: Max queue size reached, calling ProcessQueue");
            ProcessQueue();
        }

        /// <summary>
        /// Writes all messages currently in the queue to MongoDb and acknowledges
        /// </summary>
        protected override void ProcessQueue()
        {
            // Will happen when ProcessQueue is called before we receive our first message
            if (Model == null)
                return;

            lock (LockObj)
            {
                if (ToProcess.Count == 0)
                    return;

                Logger.Debug("SeriesMessageProcessor: Queue contains " + ToProcess.Count + " message to write");

                IEnumerable<string> batchDirectories = ToProcess.Select(t => t.Item1.GetValue("header")["DirectoryPath"].AsString).Distinct();
                Logger.Trace($"Writing series from directories: {string.Join(", ", batchDirectories)}");

                WriteResult seriesWriteResult = MongoDbAdapter.WriteMany(ToProcess.Select(t => t.Item1).ToList());

                // Result => Need to differentiate between connection loss and error in the data to be written
                // As well as making sure either all are written or none

                if (seriesWriteResult == WriteResult.Success)
                {
                    Logger.Debug("SeriesMessageProcessor: Wrote " + ToProcess.Count + " messages successfully, sending ACKs");

                    foreach (var (_, header, deliveryTag) in ToProcess)
                        Ack(header, deliveryTag);

                    AckCount += ToProcess.Count;
                    ToProcess.Clear();
                    FailedWriteAttempts = 0;
                }
                else
                {
                    Logger.Warn($"SeriesMessageProcessor: Failed to write {FailedWriteAttempts + 1} time(s) in a row");

                    if (++FailedWriteAttempts < FailedWriteLimit)
                        return;

                    throw new ApplicationException("Failed write attempts exceeded");
                }
            }
        }

        public override void StopProcessing(string reason)
        {
            Logger.Debug("SeriesMessageProcessor: Stopping (" + reason + ")");
            StopProcessing();
        }
    }
}
