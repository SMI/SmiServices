
using SmiServices.Common.Messages;
using SmiServices.Common.Messaging;
using SmiServices.Common.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;


namespace SmiServices.Microservices.DicomTagReader.Execution
{
    public class ParallelTagReader : TagReaderBase
    {
        private readonly ParallelOptions _parallelOptions;

        public ParallelTagReader(DicomTagReaderOptions options, FileSystemOptions fileSystemOptions,
            IProducerModel seriesMessageProducerModel, IProducerModel fileMessageProducerModel, IFileSystem fs)
            : base(options, fileSystemOptions, seriesMessageProducerModel, fileMessageProducerModel, fs)
        {
            _parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = options.MaxIoThreads
            };

            Logger.Info($"Using MaxDegreeOfParallelism={_parallelOptions.MaxDegreeOfParallelism} for parallel IO operations");
        }

        protected override List<DicomFileMessage> ReadTagsImpl(IEnumerable<FileInfo> dicomFilePaths,
            AccessionDirectoryMessage accMessage)
        {
            var fileMessages = new List<DicomFileMessage>();
            var fileMessagesLock = new object();

            Parallel.ForEach(dicomFilePaths, _parallelOptions, dicomFilePath =>
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

                    return;
                }

                lock (fileMessagesLock)
                    fileMessages.Add(fileMessage);

                Interlocked.Increment(ref NFilesProcessed);
            });

            return fileMessages;
        }
    }
}
