
using System;
using Smi.Common.Messages;
using Smi.Common.Messaging;
using Microservices.DicomTagReader.Execution;
using RabbitMQ.Client.Events;

namespace Microservices.DicomTagReader.Messaging
{
    /// <summary>
    /// Consumer class for AccessionDirectoryMessage(s)
    /// </summary>
    public class DicomTagReaderConsumer : Consumer<AccessionDirectoryMessage>
    {
        private readonly TagReaderBase _reader;


        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="reader"></param>>
        public DicomTagReaderConsumer(TagReaderBase reader)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }


        /// <summary>
        /// Callback method for received messages
        /// </summary>
        /// <param name="header">The audit trail and origin of the IMessage contained in deliverArgs</param>
        /// <param name="message">The message and associated information</param>
        /// <param name="tag"></param>
        protected override void ProcessMessageImpl(IMessageHeader header, AccessionDirectoryMessage message, ulong tag)
        {
            lock (_reader.TagReaderProcessLock)
            {
                if (_reader.IsExiting)
                    return;

                try
                {
                    _reader.ReadTags(header, message);
                }
                catch (ApplicationException e)
                {
                    // Catch specific exceptions we are aware of, any uncaught will bubble up to the wrapper in ProcessMessage

                    ErrorAndNack(header, tag, "Error while processing AccessionDirectoryMessage", e);
                    return;
                }
            }

            Ack(header, tag);
        }
    }
}
