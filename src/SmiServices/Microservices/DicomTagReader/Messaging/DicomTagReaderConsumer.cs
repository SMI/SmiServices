
using System;
using Smi.Common.Messages;
using Smi.Common.Messaging;
using System.IO;
using Smi.Common.Options;
using SmiServices.Microservices.DicomTagReader.Execution;

namespace SmiServices.Microservices.DicomTagReader.Messaging
{
    /// <summary>
    /// Consumer class for AccessionDirectoryMessage(s)
    /// </summary>
    public class DicomTagReaderConsumer : Consumer<AccessionDirectoryMessage>
    {
        private readonly TagReaderBase _reader;
        private readonly GlobalOptions _opts;


        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="dicomTagReaderOptions"></param>>
        public DicomTagReaderConsumer(TagReaderBase reader, GlobalOptions dicomTagReaderOptions)
        {
            _reader = reader;
            _opts = dicomTagReaderOptions;
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

        /// <summary>
        /// Runs a single file (dicom or zip) through tag reading process
        /// </summary>
        /// <param name="file"></param>
        public void RunSingleFile(FileInfo file)
        {
            // tell reader only to consider our specific file
            _reader.IncludeFile = f => new FileInfo(f).FullName.Equals(file.FullName, StringComparison.CurrentCultureIgnoreCase);
            _reader.ReadTags(null, new AccessionDirectoryMessage(_opts.FileSystemOptions!.FileSystemRoot!, file.Directory!));

            // good practice to clear this afterwards
            _reader.IncludeFile = null;
        }
    }
}
