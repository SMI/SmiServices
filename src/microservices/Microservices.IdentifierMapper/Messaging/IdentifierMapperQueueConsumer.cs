using Dicom;
using DicomTypeTranslation;
using Microservices.IdentifierMapper.Execution.Swappers;
using RabbitMQ.Client.Events;
using Smi.Common.Messages;
using Smi.Common.Messaging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;

namespace Microservices.IdentifierMapper.Messaging
{
    public class IdentifierMapperQueueConsumer : Consumer
    {
        public bool AllowRegexMatching { get; set; }

        private readonly IProducerModel _producer;
        private readonly ISwapIdentifiers _swapper;

        private readonly Regex _patientIdRegex = new Regex("\"00100020\":{\"vr\":\"LO\",\"Value\":\\[\"(\\d*)\"]", RegexOptions.IgnoreCase);

        BlockingCollection<Tuple<DicomFileMessage,IMessageHeader,BasicDeliverEventArgs>> msgq=new BlockingCollection<Tuple<DicomFileMessage,IMessageHeader, BasicDeliverEventArgs>>();
        private Thread acker;

        public IdentifierMapperQueueConsumer(IProducerModel producer, ISwapIdentifiers swapper)
        {
            _producer = producer;
            _swapper = swapper;
            acker=new Thread(() =>
            {
                try
                {
                    while (true)
                    {
                        List<Tuple<IMessageHeader, BasicDeliverEventArgs>> done = new List<Tuple<IMessageHeader, BasicDeliverEventArgs>>();
                        Tuple<DicomFileMessage, IMessageHeader, BasicDeliverEventArgs> t;
                        t = msgq.Take();

                        lock (_producer)
                        {
                            _producer.SendMessage(t.Item1, t.Item2, "");
                            done.Add(new Tuple<IMessageHeader, BasicDeliverEventArgs>(t.Item2, t.Item3));
                            while (msgq.TryTake(out t))
                            {
                                _producer.SendMessage(t.Item1, t.Item2, "");
                                done.Add(new Tuple<IMessageHeader, BasicDeliverEventArgs>(t.Item2, t.Item3));
                            }
                            _producer.WaitForConfirms();
                            foreach (var ack in done)
                            {
                                Ack(ack.Item1, ack.Item2);
                            }
                        }
                    }
                }
                catch (InvalidOperationException)
                {
                    // The BlockingCollection will throw this exception when closed by Shutdown()
                    return;
                }
            });
            acker.IsBackground = true;
            acker.Start();
        }

        /// <summary>
        /// Cleanly shut this process down, draining the Ack queue and ending that thread
        /// </summary>
        public override void Shutdown()
        {
            msgq.CompleteAdding();
            acker.Join();
        }

        protected override void ProcessMessageImpl(IMessageHeader header, BasicDeliverEventArgs deliverArgs)
        {
            DicomFileMessage msg;

            if (!SafeDeserializeToMessage(header, deliverArgs, out msg))
                return;

            string errorReason = null;
            var success = false;

            try
            {
                if (AllowRegexMatching)
                {
                    Match match = _patientIdRegex.Match(msg.DicomDataset);

                    //Try to do swap using regex looking for a chi (10 digits in length)
                    if (match.Success)
                    {
                        string patId = match.Groups[1].Value;
                        if (!string.IsNullOrEmpty(patId) && patId.Trim().Length == 10)
                            success = SwapIdentifier(msg, patId.Trim(), out errorReason);
                    }
                }

                if (!success)
                    success = SwapIdentifier(msg, out errorReason);
            }
            catch (ApplicationException e)
            {
                // Catch specific exceptions we are aware of, any uncaught will bubble up to the wrapper in ProcessMessage

                ErrorAndNack(header, deliverArgs, "Error while processing DicomFileMessage", e);
                return;
            }

            if (!success)
            {
                Logger.Info("Could not swap identifiers for message " + header.MessageGuid + ". Reason was: " + errorReason);
                ErrorAndNack(header, deliverArgs, errorReason, null);
            }
            else
            {
                // Enqueue the outgoing message. Request will be acked by the queue handling thread above.
                msgq.Add(new Tuple<DicomFileMessage, IMessageHeader, BasicDeliverEventArgs>(msg, header, deliverArgs));
            }
        }

        private bool SwapIdentifier(DicomFileMessage msg, string patientId, out string errorReason)
        {
            string to = _swapper.GetSubstitutionFor(patientId, out errorReason);

            if (to == null)
            {
                errorReason = "Swapper " + _swapper + " returned null";
                return false;
            }

            msg.DicomDataset = msg.DicomDataset.Replace(":[\"" + patientId + "\"]", ":[\"" + to + "\"]");

            return true;
        }

        public bool SwapIdentifier(DicomFileMessage msg, out string reason)
        {
            DicomDataset ds;
            try
            {
                ds = DicomTypeTranslater.DeserializeJsonToDataset(msg.DicomDataset);
            }
            catch (Exception e)
            {
                throw new ApplicationException("Failed to deserialize dataset", e);
            }

            if (!ds.Contains(DicomTag.PatientID))
            {
                reason = "Dataset did not contain PatientID";
                return false;
            }

            var from = (string)DicomTypeTranslaterReader.GetCSharpValue(ds, DicomTag.PatientID);

            if (string.IsNullOrWhiteSpace(from))
            {
                reason = "PatientID was blank";
                return false;
            }

            string to = _swapper.GetSubstitutionFor(from, out reason);

            if (to == null)
            {
                reason = "Swapper " + _swapper + " returned null";
                return false;
            }

            // Update the JSON deserialized dicom dataset
            ds.AddOrUpdate(DicomTag.PatientID, to);

            string updatedJson;

            try
            {
                updatedJson = DicomTypeTranslater.SerializeDatasetToJson(ds);
            }
            catch (Exception e)
            {
                throw new ApplicationException("Failed to serialize dataset", e);
            }


            // Unlikely, but should still check
            if (updatedJson == null)
            {
                reason = "Updated json string was null";
                return false;
            }

            // Override the message DicomDataset with the new serialized dataset
            msg.DicomDataset = updatedJson;

            return true;
        }
    }
}
