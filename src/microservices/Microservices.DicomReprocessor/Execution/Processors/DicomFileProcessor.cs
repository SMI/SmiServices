
using FellowOakDicom;
using DicomTypeTranslation;
using MongoDB.Bson;
using NLog;
using Smi.Common.Messages;
using Smi.Common.Messaging;
using Smi.Common.MongoDB;
using Smi.Common.Options;
using System;
using System.Collections.Generic;
using System.Threading;


namespace Microservices.DicomReprocessor.Execution.Processors
{
    /// <summary>
    /// Processes whole BsonDocuments (whole dicom files) into DicomFileMessage(s) and publishes them to config-defined image exchange
    /// </summary>
    public class DicomFileProcessor : IDocumentProcessor
    {
        public long TotalProcessed { get; private set; }

        public long TotalFailed
        {
            get
            {
                // Simple way of getting value of _totalFailed in an atomic op.
                return Interlocked.CompareExchange(ref _totalFailed, 0, 0);
            }
        }


        /// <summary>
        /// Backing field for TotalFailed. Only incremented in an atomic context
        /// </summary>
        private long _totalFailed;

        private readonly ILogger _logger;

        private readonly DicomReprocessorOptions _options;

        private readonly IProducerModel _producerModel;
        private readonly string _reprocessingRoutingKey;

        private List<Tuple<DicomFileMessage, IMessageHeader>> _messageBuffer = new();
        private readonly object _oBufferLock = new();


        public DicomFileProcessor(DicomReprocessorOptions options, IProducerModel producerModel, string reprocessingRoutingKey)
        {
            _logger = LogManager.GetLogger(GetType().Name);

            _options = options;

            _producerModel = producerModel;
            _reprocessingRoutingKey = reprocessingRoutingKey;
        }


        public void ProcessDocument(BsonDocument document)
        {
            string documentId = document["_id"].ToString()!;

            var headerDoc = document["header"] as BsonDocument;

            if (headerDoc == null)
            {
                LogUnprocessedDocument(documentId, new ApplicationException("Document did not contain a header field"));
                return;
            }

            var message = new DicomFileMessage
            {
                DicomFilePath = (string)headerDoc["DicomFilePath"],
                DicomFileSize = headerDoc.Contains("DicomFileSize") ? (long)headerDoc["DicomFileSize"] : -1
            };

            try
            {
                // Rebuild the dataset from the document, then serialize it to JSON to send
                DicomDataset ds = DicomTypeTranslaterWriter.BuildDicomDataset(document);
                message.DicomDataset = DicomTypeTranslater.SerializeDatasetToJson(ds);

                // Add the header information
                message.StudyInstanceUID = ds.GetValue<string>(DicomTag.StudyInstanceUID, 0);
                message.SeriesInstanceUID = ds.GetValue<string>(DicomTag.SeriesInstanceUID, 0);
                message.SOPInstanceUID = ds.GetValue<string>(DicomTag.SOPInstanceUID, 0);
            }
            catch (Exception e)
            {
                LogUnprocessedDocument(documentId, e);
                return;
            }

            if (!message.VerifyPopulated())
            {
                LogUnprocessedDocument(documentId, new ApplicationException("Message was not valid"));
                return;
            }

            IMessageHeader header = MongoDocumentHeaders.RebuildMessageHeader(headerDoc["MessageHeader"].AsBsonDocument);

            lock (_oBufferLock)
                _messageBuffer.Add(new Tuple<DicomFileMessage, IMessageHeader>(message, header));
        }

        public void SendMessages()
        {
            _logger.Debug("Sending messages in buffer");

            lock (_oBufferLock)
            {
                var newBatchHeaders = new List<IMessageHeader>();
                foreach ((DicomFileMessage message, IMessageHeader header) in _messageBuffer)
                    newBatchHeaders.Add(_producerModel.SendMessage(message, header, _reprocessingRoutingKey));

                // Confirm all messages in the batch
                _producerModel.WaitForConfirms();

                TotalProcessed += _messageBuffer.Count;

                foreach (IMessageHeader newHeader in newBatchHeaders)
                    newHeader.Log(_logger, LogLevel.Trace, "Sent");

                _messageBuffer.Clear();
            }
        }

        public void LogProgress() => _logger.Info($"Total messages sent: {TotalProcessed}. Total failed to reprocess: {TotalFailed}");

        private void LogUnprocessedDocument(string documentId, Exception e)
        {
            _logger.Error(e, "Error when processing document with _id " + documentId);
            Interlocked.Increment(ref _totalFailed);
        }
    }
}
