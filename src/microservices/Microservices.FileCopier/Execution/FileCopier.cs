using JetBrains.Annotations;
using NLog;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.Messaging;
using System.IO.Abstractions;


namespace Microservices.FileCopier.Execution
{
    public class FileCopier : IFileCopier
    {
        [NotNull] private readonly IProducerModel _copyStatusProducerModel;

        [NotNull] private readonly string _fileSystemRoot;
        [NotNull] private readonly IFileSystem _fileSystem;

        [NotNull] private readonly ILogger _logger;


        public FileCopier(
            [NotNull] IProducerModel copyStatusCopyStatusProducerModel,
            [NotNull] string fileSystemRoot,
            [CanBeNull] IFileSystem fileSystem = null)
        {
            _copyStatusProducerModel = copyStatusCopyStatusProducerModel;
            _fileSystemRoot = fileSystemRoot;
            _fileSystem = fileSystem ?? new FileSystem();

            _logger = LogManager.GetLogger(GetType().Name);

        }

        public void ProcessMessage(
            [NotNull] ExtractFileMessage message,
            [NotNull] IMessageHeader header)
        {
            string fullSrc = _fileSystem.Path.Join(_fileSystemRoot, message.DicomFilePath);

            ExtractFileStatusMessage statusMessage;

            if (!_fileSystem.File.Exists(fullSrc))
            {
                statusMessage = new ExtractFileStatusMessage(message)
                {
                    DicomFilePath = message.DicomFilePath,
                    Status = ExtractFileStatus.FileMissing,
                    StatusMessage = $"Could not find '{fullSrc}'"
                };
                _copyStatusProducerModel.SendMessage(statusMessage, header);
                return;
            }

            string fullDest = _fileSystem.Path.Join(_fileSystemRoot, message.OutputPath);

            if (_fileSystem.File.Exists(fullDest))
                _logger.Warn($"Output file '{fullDest}' already exists. Will overwrite.");

            IDirectoryInfo parent = _fileSystem.Directory.GetParent(fullDest);
            if (!parent.Exists)
            {
                _logger.Debug($"Creating directory '{parent}'");
                parent.Create();
            }

            _logger.Debug($"Copying source file to '{message.OutputPath}'");
            _fileSystem.File.Copy(fullSrc, fullDest, overwrite: true);

            statusMessage = new ExtractFileStatusMessage(message)
            {
                DicomFilePath = message.DicomFilePath,
                Status = ExtractFileStatus.Copied,
                AnonymisedFileName = message.OutputPath,
            };
            _copyStatusProducerModel.SendMessage(statusMessage, header);
        }
    }
}
