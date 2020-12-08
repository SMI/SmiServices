using JetBrains.Annotations;
using NLog;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.Messaging;
using Smi.Common.Options;
using System;
using System.IO.Abstractions;


namespace Microservices.FileCopier.Execution
{
    public class ExtractionFileCopier : IFileCopier
    {
        [NotNull] private readonly FileCopierOptions _options;

        [NotNull] private readonly IProducerModel _copyStatusProducerModel;

        [NotNull] private readonly string _fileSystemRoot;
        [NotNull] private readonly string _extractionRoot;
        [NotNull] private readonly IFileSystem _fileSystem;

        [NotNull] private readonly ILogger _logger;


        public ExtractionFileCopier(
            [NotNull] FileCopierOptions options,
            [NotNull] IProducerModel copyStatusCopyStatusProducerModel,
            [NotNull] string fileSystemRoot,
            [NotNull] string extractionRoot,
            [CanBeNull] IFileSystem fileSystem = null)
        {
            _options = options;
            _copyStatusProducerModel = copyStatusCopyStatusProducerModel;
            _fileSystemRoot = fileSystemRoot;
            _extractionRoot = extractionRoot;
            _fileSystem = fileSystem ?? new FileSystem();

            if (!_fileSystem.Directory.Exists(_fileSystemRoot))
                throw new ArgumentException($"Cannot find the specified fileSystemRoot: '{_fileSystemRoot}'");
            if (!_fileSystem.Directory.Exists(_extractionRoot))
                throw new ArgumentException($"Cannot find the specified extractionRoot: '{_extractionRoot}'");

            _logger = LogManager.GetLogger(GetType().Name);
            _logger.Info($"fileSystemRoot={_fileSystemRoot}, extractionRoot={_extractionRoot}");
        }

        public void ProcessMessage(
            [NotNull] ExtractFileMessage message,
            [NotNull] IMessageHeader header)
        {
            string fullSrc = _fileSystem.Path.Combine(_fileSystemRoot, message.DicomFilePath);

            ExtractedFileStatusMessage statusMessage;

            if (!_fileSystem.File.Exists(fullSrc))
            {
                statusMessage = new ExtractedFileStatusMessage(message)
                {
                    DicomFilePath = message.DicomFilePath,
                    Status = ExtractedFileStatus.FileMissing,
                    StatusMessage = $"Could not find '{fullSrc}'"
                };
                _ = _copyStatusProducerModel.SendMessage(statusMessage, header, _options.NoVerifyRoutingKey);
                return;
            }

            string fullDest = _fileSystem.Path.Combine(_extractionRoot, message.ExtractionDirectory, message.OutputPath);

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

            statusMessage = new ExtractedFileStatusMessage(message)
            {
                DicomFilePath = message.DicomFilePath,
                Status = ExtractedFileStatus.Copied,
                OutputFilePath = message.OutputPath,
            };
            _ = _copyStatusProducerModel.SendMessage(statusMessage, header, _options.NoVerifyRoutingKey);
        }
    }
}
