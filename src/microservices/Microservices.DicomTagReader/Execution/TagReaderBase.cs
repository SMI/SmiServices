
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using FellowOakDicom;
using DicomTypeTranslation;
using Smi.Common.Messages;
using Smi.Common.Messaging;
using Smi.Common.Options;
using NLog;
using System.IO.Compression;
using Smi.Common;

namespace Microservices.DicomTagReader.Execution
{
    public abstract class TagReaderBase
    {
        private readonly string _filesystemRoot;
        private readonly IFileSystem _fs;

        private readonly IProducerModel _seriesMessageProducerModel;
        private readonly IProducerModel _fileMessageProducerModel;
        protected readonly ILogger Logger;

        protected readonly bool NackIfAnyFileErrors;

        private readonly string _searchPattern;

        private static FileReadOption _fileReadOption;

        private readonly Stopwatch _stopwatch = new();
        private int _nAccMessagesProcessed;
        protected int NFilesProcessed;
        private int _nMessagesSent;
        private readonly long[] _swTotals = new long[4]; // Enumerate, Read, Send, Total

        public bool IsExiting;
        public readonly object TagReaderProcessLock = new();
        
        /// <summary>
        /// Optional function for last minute filtering of which files in an <see cref="AccessionDirectoryMessage"/> folder get processed
        /// </summary>
        public Func<string,bool>? IncludeFile {get;set;}

        /// <summary>
        /// Interrogates directory tree for dicom files and produces series info and individual file info
        /// </summary>
        /// <param name="options"></param>
        /// <param name="fileSystemOptions"></param>
        /// <param name="seriesMessageProducerModel"></param>
        /// <param name="fileMessageProducerModel"></param>
        /// <param name="fs">File system to use</param>
        public TagReaderBase(DicomTagReaderOptions options, FileSystemOptions fileSystemOptions, IProducerModel seriesMessageProducerModel, IProducerModel fileMessageProducerModel, IFileSystem fs)
        {
            Logger = LogManager.GetLogger(GetType().Name);

            _filesystemRoot = fileSystemOptions.FileSystemRoot ?? throw new ArgumentNullException(nameof(fileSystemOptions.FileSystemRoot));
            NackIfAnyFileErrors = options.NackIfAnyFileErrors;
            _searchPattern = fileSystemOptions.DicomSearchPattern ?? throw new ArgumentNullException(nameof(fileSystemOptions.DicomSearchPattern));

            _fileReadOption = options.GetReadOption();

            Logger.Debug($"FileReadOption is: {_fileReadOption}");

            _seriesMessageProducerModel = seriesMessageProducerModel;
            _fileMessageProducerModel = fileMessageProducerModel;
            _fs = fs;

            Logger.Info($"Stopwatch implementation - IsHighResolution: {Stopwatch.IsHighResolution}. Frequency: {Stopwatch.Frequency} ticks/s");
        }

        /// <summary>
        /// Process files from the directory referenced in the message
        /// </summary>
        /// <param name="header"></param>
        /// <param name="message"></param>
        public void ReadTags(IMessageHeader? header, AccessionDirectoryMessage message)
        {
            _stopwatch.Restart();

            string dirPath = message.GetAbsolutePath(_filesystemRoot);
            Logger.Debug("TagReader: About to process files in " + dirPath);

            if (!_fs.Directory.Exists(dirPath))
                throw new ApplicationException("Directory not found: " + dirPath);

            if (!dirPath.StartsWith(_filesystemRoot, StringComparison.CurrentCultureIgnoreCase))
                throw new ApplicationException("Directory " + dirPath + " is not below the given FileSystemRoot (" +
                                               _filesystemRoot + ")");
            long beginEnumerate = _stopwatch.ElapsedTicks;
            string[] dicomFilePaths = _fs.Directory.EnumerateFiles(dirPath, _searchPattern).Where(Include).ToArray();
            string[] zipFilePaths =  _fs.Directory.EnumerateFiles(dirPath).Where(ZipHelper.IsZip).Where(Include).ToArray();

            _swTotals[0] += _stopwatch.ElapsedTicks - beginEnumerate;
            Logger.Debug("TagReader: Found " + dicomFilePaths.Length + " dicom files to process");
            Logger.Debug("TagReader: Found " + zipFilePaths.Length + " zip files to process");

            int toProcess = dicomFilePaths.Length + zipFilePaths.Length;

            if (toProcess == 0)
                throw new ApplicationException("No dicom/zip files found in " + dirPath);

            // We have files to process, let's do it!

            long beginRead = _stopwatch.ElapsedTicks;

            List<DicomFileMessage> fileMessages = ReadTagsImpl(dicomFilePaths.Select(p=>new FileInfo(p)), message);
            fileMessages.AddRange(ReadZipFilesImpl(zipFilePaths.Select(p=>new FileInfo(p)), message));

            _swTotals[1] += (_stopwatch.ElapsedTicks - beginRead) / toProcess;

            var seriesMessages = new Dictionary<string, SeriesMessage>();

            foreach (DicomFileMessage fileMessage in fileMessages)
            {
                var seriesUID = fileMessage.SeriesInstanceUID;

                // If we've already seen this seriesUID, just update the image count
                if (seriesMessages.TryGetValue(seriesUID, out var c))
                {
                    c.ImagesInSeries++;
                    continue;
                }

                // Else create a new SeriesMessage
                var seriesMessage = new SeriesMessage
                {
                    DirectoryPath = message.DirectoryPath,

                    StudyInstanceUID = fileMessage.StudyInstanceUID,
                    SeriesInstanceUID = seriesUID,

                    ImagesInSeries = 1,

                    DicomDataset = fileMessage.DicomDataset
                };

                seriesMessages.Add(seriesUID, seriesMessage);
            }

            Logger.Debug("TagReader: Finished processing directory, sending messages");

            // Only send if have processed all files in the directory ok

            if (!fileMessages.Any())
                throw new ApplicationException("No DicomFileMessage(s) to send after processing the directory");

            if (!seriesMessages.Any())
                throw new ApplicationException("No SeriesMessage(s) to send but we have file messages");

            Logger.Info($"Sending {fileMessages.Count} DicomFileMessage(s)");

            long beginSend = _stopwatch.ElapsedTicks;
            var headers = new List<IMessageHeader>();
            foreach (DicomFileMessage fileMessage in fileMessages)
                headers.Add(_fileMessageProducerModel.SendMessage(fileMessage, header, routingKey: null));

            _fileMessageProducerModel.WaitForConfirms();

            headers.ForEach(x => x.Log(Logger, LogLevel.Trace, $"Sent {header?.MessageGuid}"));

            Logger.Info($"Sending {seriesMessages.Count} SeriesMessage(s)");

            headers.Clear();
            foreach (KeyValuePair<string, SeriesMessage> kvp in seriesMessages)
                headers.Add(_seriesMessageProducerModel.SendMessage(kvp.Value, header, routingKey: null));

            _seriesMessageProducerModel.WaitForConfirms();
            headers.ForEach(x => x.Log(Logger, LogLevel.Trace, $"Sent {x.MessageGuid}"));

            _swTotals[2] += _stopwatch.ElapsedTicks - beginSend;
            _swTotals[3] += _stopwatch.ElapsedTicks;
            _nMessagesSent += fileMessages.Count + seriesMessages.Count;

            if (++_nAccMessagesProcessed % 10 == 0)
                LogRates();
        }


        public bool Include(string filePath)
        {
            return IncludeFile?.Invoke(filePath) ?? true;
        }

        /// <summary>
        /// Opens all zip files <paramref name="zipFilePaths"/> and generates a <see cref="DicomFileMessage"/> for each dcm file in the archive
        /// </summary>
        /// <param name="zipFilePaths">All the zip files that must be explored for dcm files</param>
        /// <param name="accMessage">The upstream message that suggested we look for dicom files in a given directory</param>
        /// <returns></returns>
        protected virtual IEnumerable<DicomFileMessage> ReadZipFilesImpl(IEnumerable<FileInfo> zipFilePaths,AccessionDirectoryMessage accMessage)
        {
            foreach(FileInfo zipFilePath in zipFilePaths)
            {
                using var archive = ZipFile.Open(zipFilePath.FullName,ZipArchiveMode.Read);
                foreach(var entry in archive.Entries)
                {
                    if (!entry.FullName.EndsWith(".dcm", StringComparison.CurrentCultureIgnoreCase)) continue;
                    byte[]? buffer = null;
                    
                    buffer = ReadFully(entry.Open());

                    using var memoryStream = new MemoryStream(buffer);
                    var dicom = DicomFile.Open(memoryStream);

                    yield return DicomFileToMessage(dicom.Dataset, $"{zipFilePath.FullName}!{entry.FullName}",null);
                }
            }
        }

        /// <summary>
        /// Creates a new <see cref="DicomFileMessage"/> by reading <paramref name="ds"/> tags
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="dicomFilePath">The full path that <paramref name="ds"/> was read from</param>
        /// <param name="fileSize">File size if known otherwise null</param>
        /// <returns></returns>
        /// <exception cref="ApplicationException">If <paramref name="ds"/> is missing required UIDS or serializing the dataset went wrong</exception>
        protected DicomFileMessage DicomFileToMessage(DicomDataset ds, string dicomFilePath, long? fileSize)
        {
            var IDs = new string[3];

            try
            {
                // Pre-fetch these to ensure they exist before we go further
                IDs[0] = ds.GetValue<string>(DicomTag.StudyInstanceUID, 0);
                IDs[1] = ds.GetValue<string>(DicomTag.SeriesInstanceUID, 0);
                IDs[2] = ds.GetValue<string>(DicomTag.SOPInstanceUID, 0);

                if (IDs.Any(string.IsNullOrWhiteSpace))
                    throw new DicomDataException("A required ID tag existed but its value was invalid");
            }
            catch (DicomDataException dde)
            {
                throw new ApplicationException("File opened but had a missing ID", dde);
            }

            string serializedDataset;

            try
            {
                DicomDataset filtered = new(ds.Where(i => i is not DicomOtherByteFragment).ToArray());
                serializedDataset = DicomTypeTranslater.SerializeDatasetToJson(filtered);
            }
            catch (Exception e)
            {
                throw new ApplicationException("Failed to serialize dataset", e);
            }

            return new DicomFileMessage(_filesystemRoot, dicomFilePath)
            {
                //TODO(Ruairidh 04/07) Where are these used?
                StudyInstanceUID = IDs[0],
                SeriesInstanceUID = IDs[1],
                SOPInstanceUID = IDs[2],

                DicomDataset = serializedDataset,
                DicomFileSize = fileSize ?? -1
            };
            
        }

        private byte[] ReadFully(Stream stream)
        {
            var buffer = new byte[32768];
            int len;
            try
            {
                len = (int)stream.Length;
            }
            catch
            {
                len = 32768;
            }
            using var ms = new MemoryStream(len);
            while (true)
            {
                var read = stream.Read(buffer, 0, buffer.Length);
                if (read <= 0)
                    return ms.ToArray();
                ms.Write(buffer, 0, read);
            }
        }
        protected abstract List<DicomFileMessage> ReadTagsImpl(IEnumerable<FileInfo> dicomFilePaths,
            AccessionDirectoryMessage accMessage);

        /// <summary>
        /// Builds a <see cref="DicomFileMessage"/> from a single dicom file
        /// </summary>
        /// <param name="dicomFilePath"></param>
        /// <returns></returns>
        protected DicomFileMessage ReadTagsFromFile(FileInfo dicomFilePath)
        {
            try
            {
                return DicomFileToMessage(DicomFile.Open(dicomFilePath.FullName, _fileReadOption).Dataset,dicomFilePath.FullName,dicomFilePath.Length);
            }
            catch (DicomFileException dfe)
            {
                throw new ApplicationException($"Could not open dicom file: {dicomFilePath}", dfe);
            }
        }

        private void LogRates()
        {
            if (_nAccMessagesProcessed == 0)
            {
                Logger.Info("No messages processed - can't calculate averages");
                return;
            }

            long freq = Stopwatch.Frequency;
            var sb = new StringBuilder("Average rates - ");
            sb.Append($"enumerate dir (per acc. message): {_swTotals[0] * 1.0 / (double)(freq * _nAccMessagesProcessed):f6}s, ");
            sb.Append($"file process: {_swTotals[1] * 1.0 / (double)(freq * NFilesProcessed):f6}s, ");
            sb.Append($"send messages: {_swTotals[2] * 1.0 / (double)(freq * _nMessagesSent):f6}s, ");
            sb.Append($"overall: {_swTotals[3] * 1.0 / (double)(freq * _nAccMessagesProcessed):f6}s");
            Logger.Info(sb.ToString);
        }

        public void Stop()
        {
            lock (TagReaderProcessLock)
                IsExiting = true;

            Logger.Info("Lock released, no more messages will be processed");

            LogRates();
        }
    }
}
