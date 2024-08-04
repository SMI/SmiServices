
using SmiServices.Common.Messages;
using SmiServices.Common.Messaging;
using SmiServices.Common.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;


namespace SmiServices.Microservices.DicomTagReader.Execution
{
    public class SerialTagReader : TagReaderBase
    {
        public SerialTagReader(DicomTagReaderOptions options, FileSystemOptions fileSystemOptions,
            IProducerModel seriesMessageProducerModel, IProducerModel fileMessageProducerModel, IFileSystem fs)
            : base(options, fileSystemOptions, seriesMessageProducerModel, fileMessageProducerModel, fs) { }

        protected override List<DicomFileMessage> ReadTagsImpl(IEnumerable<FileInfo> dicomFilePaths, AccessionDirectoryMessage accMessage)
        {
            var fileMessages = new List<DicomFileMessage>();

            foreach (FileInfo dicomFilePath in dicomFilePaths)
            {
                Logger.Trace("TagReader: Processing " + dicomFilePath);

                DicomFileMessage fileMessage;

                try
                {
                    fileMessage = ReadTagsFromFile(dicomFilePath);
                }
                catch (Exception e)
                {
                    if (NackIfAnyFileErrors)
                        throw new ApplicationException(
                            "Exception processing file and NackIfAnyFileErrors option set. File was: " + dicomFilePath,
                            e);

                    Logger.Error(e,
                        "Error processing file " + dicomFilePath +
                        ". Ignoring and moving on since NackIfAnyFileErrors is false");

                    continue;
                }

                fileMessages.Add(fileMessage);
                ++NFilesProcessed;
            }

            return fileMessages;
        }
    }
}
