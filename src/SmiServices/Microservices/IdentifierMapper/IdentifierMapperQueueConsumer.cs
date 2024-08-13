using DicomTypeTranslation;
using FellowOakDicom;
using SmiServices.Common.Messages;
using SmiServices.Common.Messaging;
using SmiServices.Microservices.IdentifierMapper.Swappers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace SmiServices.Microservices.IdentifierMapper
{
    public class IdentifierMapperQueueConsumer : Consumer<DicomFileMessage>
    {
        public bool AllowRegexMatching { get; set; }

        private readonly IProducerModel _producer;
        private readonly ISwapIdentifiers _swapper;

        private readonly Regex _patientIdRegex = new("\"00100020\":{\"vr\":\"LO\",\"Value\":\\[\"(\\d*)\"]", RegexOptions.IgnoreCase);

        private readonly BlockingCollection<Tuple<DicomFileMessage, IMessageHeader, ulong>> msgq = [];
        private readonly Thread acker;

        public IdentifierMapperQueueConsumer(IProducerModel producer, ISwapIdentifiers swapper)
        {
            _producer = producer;
            _swapper = swapper;
            acker = new Thread(() =>
              {
                  try
                  {
                      while (true)
                      {
                          List<Tuple<IMessageHeader, ulong>> done = [];
                          Tuple<DicomFileMessage, IMessageHeader, ulong> t;
                          t = msgq.Take();

                          lock (_producer)
                          {
                              _producer.SendMessage(t.Item1, t.Item2, "");
                              done.Add(new Tuple<IMessageHeader, ulong>(t.Item2, t.Item3));
                              while (msgq.TryTake(out t!))
                              {
                                  _producer.SendMessage(t.Item1, t.Item2, "");
                                  done.Add(new Tuple<IMessageHeader, ulong>(t.Item2, t.Item3));
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
              })
            {
                IsBackground = true
            };
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

        protected override void ProcessMessageImpl(IMessageHeader header, DicomFileMessage msg, ulong tag)
        {
            string? errorReason = null;
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
            catch (BadPatientIDException e)
            {
                ErrorAndNack(header, tag, "Error while processing DicomFileMessage", e);
                return;
            }
            catch (ApplicationException e)
            {
                // Catch specific exceptions we are aware of, any uncaught will bubble up to the wrapper in ProcessMessage

                ErrorAndNack(header, tag, "Error while processing DicomFileMessage", e);
                return;
            }

            if (!success)
            {
                Logger.Info($"Could not swap identifiers for message {header.MessageGuid}. Reason was: {errorReason}");
                ErrorAndNack(header, tag, errorReason!, new Exception());
            }
            else
            {
                // Enqueue the outgoing message. Request will be acked by the queue handling thread above.
                msgq.Add(new Tuple<DicomFileMessage, IMessageHeader, ulong>(msg, header, tag));
            }
        }

        private bool SwapIdentifier(DicomFileMessage msg, string patientId, out string? errorReason)
        {
            string? to = _swapper.GetSubstitutionFor(patientId, out errorReason);

            if (to == null)
            {
                errorReason = $"Swapper {_swapper} returned null";
                return false;
            }

            msg.DicomDataset = msg.DicomDataset.Replace($":[\"{patientId}\"]", $":[\"{to}\"]");

            return true;
        }

        /// <summary>
        /// Swaps the patient ID in the <paramref name="msg"/> for its anonymous mapping.  Returns true if a mapping
        /// was found or false if it was not possible to get a mapping for some reason (e.g. tag is missing or no mapping
        /// was found).
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        /// <exception cref="BadPatientIDException">Thrown if PatientID tag is corrupt</exception>
        public bool SwapIdentifier(DicomFileMessage msg, [NotNullWhen(false)] out string? reason)
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

            var from = GetPatientID(ds);

            if (string.IsNullOrWhiteSpace(from))
            {
                reason = "PatientID was blank";
                return false;
            }

            string? to = _swapper.GetSubstitutionFor(from, out reason);

            if (to == null)
            {
                reason = $"Swapper {_swapper} returned null";
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

        private static string? GetPatientID(DicomDataset ds)
        {
            var val = DicomTypeTranslaterReader.GetCSharpValue(ds, DicomTag.PatientID);

            switch (val)
            {
                case null:
                    return null;
                case string s:
                    return s;
                case string[] arr:
                    {
                        var unique = arr.Where(a => !string.IsNullOrWhiteSpace(a)).Distinct().ToArray();
                        if (unique.Length == 0)
                            return null;
                        if (unique.Length == 1)
                            return unique[0];
                        throw new BadPatientIDException($"DicomDataset had multiple values for PatientID:{string.Join("\\", arr)}");
                    }
                default:
                    throw new BadPatientIDException($"DicomDataset had bad Type for PatientID:{val.GetType()}");
            }
        }
    }
}
