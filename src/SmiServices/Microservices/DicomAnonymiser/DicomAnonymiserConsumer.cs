using FellowOakDicom;
using NLog;
using SmiServices.Common.Messages;
using SmiServices.Common.Messages.Extraction;
using SmiServices.Common.Messaging;
using SmiServices.Common.Options;
using SmiServices.Microservices.DicomAnonymiser.Anonymisers;
using System;
using System.IO;
using System.IO.Abstractions;

namespace SmiServices.Microservices.DicomAnonymiser;

public class DicomAnonymiserConsumer : Consumer<ExtractFileMessage>
{
    // TODO (da 2024-02-23) Additional Requirement: Message Batching
    // https://github.com/SMI/SmiServices/blob/main/src/microservices/Microservices.CohortPackager/Messaging/AnonVerificationMessageConsumer.cs#L72
    // https://github.com/SMI/SmiServices/blob/main/src/microservices/Microservices.MongoDbPopulator/Messaging/MongoDbPopulatorMessageConsumer.cs#L56

    private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
    private readonly DicomAnonymiserOptions _options;
    private readonly IFileSystem _fileSystem;
    private readonly string _fileSystemRoot;
    private readonly string _extractRoot;
    private readonly IDicomAnonymiser _anonymiser;
    private readonly IProducerModel _statusMessageProducer;

    public DicomAnonymiserConsumer(
        DicomAnonymiserOptions options,
        string fileSystemRoot,
        string extractRoot,
        IDicomAnonymiser anonymiser,
        IProducerModel statusMessageProducer,
        IFileSystem? fileSystem = null
    )
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _fileSystemRoot = fileSystemRoot ?? throw new ArgumentNullException(nameof(fileSystemRoot));
        _extractRoot = extractRoot ?? throw new ArgumentNullException(nameof(extractRoot));
        _anonymiser = anonymiser ?? throw new ArgumentNullException(nameof(anonymiser));
        _statusMessageProducer = statusMessageProducer ?? throw new ArgumentNullException(nameof(statusMessageProducer));
        _fileSystem = fileSystem ?? new FileSystem();

        if (!_fileSystem.Directory.Exists(_fileSystemRoot))
            throw new Exception($"Filesystem root does not exist: '{fileSystemRoot}'");

        if (!_fileSystem.Directory.Exists(_extractRoot))
            throw new Exception($"Extract root does not exist: '{extractRoot}'");
    }

    protected override void ProcessMessageImpl(IMessageHeader header, ExtractFileMessage message, ulong tag)
    {
        if (message.IsIdentifiableExtraction)
            throw new Exception("DicomAnonymiserConsumer should not handle identifiable extraction messages");

        var statusMessage = new ExtractedFileStatusMessage(message);

        var sourceFileAbs = _fileSystem.FileInfo.New(_fileSystem.Path.Combine(_fileSystemRoot, message.DicomFilePath));

        if (!sourceFileAbs.Exists)
        {
            statusMessage.Status = ExtractedFileStatus.FileMissing;
            statusMessage.StatusMessage = $"Could not find file to anonymise: '{sourceFileAbs}'";
            statusMessage.OutputFilePath = null;
            _statusMessageProducer.SendMessage(statusMessage, header, _options.RoutingKeyFailure);

            Ack(header, tag);
            return;
        }

        if (_options.FailIfSourceWriteable && !sourceFileAbs.Attributes.HasFlag(FileAttributes.ReadOnly))
        {
            statusMessage.Status = ExtractedFileStatus.ErrorWontRetry;
            statusMessage.StatusMessage = $"Source file was writeable and FailIfSourceWriteable is set: '{sourceFileAbs}'";
            statusMessage.OutputFilePath = null;
            _statusMessageProducer.SendMessage(statusMessage, header, _options.RoutingKeyFailure);

            Ack(header, tag);
            return;
        }

        var extractionDirAbs = _fileSystem.Path.Combine(_extractRoot, message.ExtractionDirectory);

        // NOTE(rkm 2021-12-07) Since this directory should have already been created, we treat this more like an assertion and throw if not found.
        // This helps prevent a flood of messages if e.g. the filesystem is temporarily unavailable.
        if (!_fileSystem.Directory.Exists(extractionDirAbs))
            throw new DirectoryNotFoundException($"Expected extraction directory to exist: '{extractionDirAbs}'");

        var destFileAbs = _fileSystem.FileInfo.New(_fileSystem.Path.Combine(extractionDirAbs, message.OutputPath));

        destFileAbs.Directory!.Create();

        _logger.Debug($"Anonymising '{sourceFileAbs}' to '{destFileAbs}'");

        // TODO (rkm 2024-02-09) Extract modality from cohort extractor instead of opening DICOM file
        DicomFile dicomFile = DicomFile.Open(sourceFileAbs.FullName);
        message.Modality = dicomFile.Dataset.GetSingleValue<string>(DicomTag.Modality);

        Console.WriteLine("[DICOM] Modality: " + message.Modality);
        Console.WriteLine("[DICOM] Source File: " + message.DicomFilePath);
        Console.WriteLine("[DICOM] Dest File: " + message.OutputPath);

        ExtractedFileStatus anonymiserStatus = ExtractedFileStatus.None;
        string anonymiserStatusMessage = "";

        try
        {
            anonymiserStatus = _anonymiser.Anonymise(message, sourceFileAbs, destFileAbs, out anonymiserStatusMessage);
        }
        catch (Exception e)
        {
            var msg = $"Error anonymising '{sourceFileAbs}'";
            _logger.Error(e, msg);

            statusMessage.Status = ExtractedFileStatus.ErrorWontRetry;
            statusMessage.StatusMessage = $"{msg}. Exception message: {e.Message}";
            statusMessage.OutputFilePath = null;
            _statusMessageProducer.SendMessage(statusMessage, header, _options.RoutingKeyFailure);

            Ack(header, tag);
        }

        string routingKey;

        if (anonymiserStatus == ExtractedFileStatus.Anonymised)
        {
            _logger.Info($"Anonymisation of '{sourceFileAbs}' successful");
            statusMessage.Status = anonymiserStatus;
            statusMessage.StatusMessage = anonymiserStatusMessage;
            routingKey = _options.RoutingKeySuccess ?? "verify";
        }
        else
        {
            _logger.Info($"Anonymisation of '{sourceFileAbs}' failed");
            statusMessage.OutputFilePath = null;
            statusMessage.Status = anonymiserStatus;
            statusMessage.StatusMessage = anonymiserStatusMessage;
            routingKey = _options.RoutingKeyFailure ?? "noverify";
        }

        _statusMessageProducer.SendMessage(statusMessage, header, routingKey);

        Ack(header, tag);
        return;
    }
}
