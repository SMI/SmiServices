using NLog;
using SmiServices.Common.Messages.Extraction;
using SmiServices.Common.Options;
using SmiServices.Microservices.DicomAnonymiser.Helpers;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace SmiServices.Microservices.DicomAnonymiser.Anonymisers;

internal class SmiCtpAnonymiser : IDicomAnonymiser, IDisposable
{
    private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
    private readonly Process _ctpProcess;

    public SmiCtpAnonymiser(GlobalOptions globalOptions)
    {
        var dicomAnonymiserOptions = globalOptions.DicomAnonymiserOptions ?? throw new ArgumentException($"{nameof(globalOptions.DicomAnonymiserOptions)} was null", nameof(globalOptions));

        if (!File.Exists(dicomAnonymiserOptions.CtpAnonCliJar))
            throw new ArgumentException($"{nameof(dicomAnonymiserOptions.CtpAnonCliJar)} '{dicomAnonymiserOptions.CtpAnonCliJar}' does not exist", nameof(globalOptions));

        if (!File.Exists(dicomAnonymiserOptions.CtpAllowlistScript))
            throw new ArgumentException($"{nameof(dicomAnonymiserOptions.CtpAllowlistScript)} '{dicomAnonymiserOptions.CtpAllowlistScript}' does not exist", nameof(globalOptions));

        var srAnonTool = "false";
        if (!string.IsNullOrWhiteSpace(dicomAnonymiserOptions.SRAnonymiserToolPath))
        {
            if (!File.Exists(dicomAnonymiserOptions.SRAnonymiserToolPath))
                throw new ArgumentException($"{nameof(dicomAnonymiserOptions.SRAnonymiserToolPath)} '{dicomAnonymiserOptions.SRAnonymiserToolPath}' does not exist", nameof(globalOptions));
            srAnonTool = dicomAnonymiserOptions.SRAnonymiserToolPath;
        }

        var ctpArgs = $"-jar {dicomAnonymiserOptions.CtpAnonCliJar} --anon-script {dicomAnonymiserOptions.CtpAllowlistScript} --sr-anon-tool {srAnonTool} --daemonize";
        _ctpProcess = ProcessWrapper.CreateProcess("java", ctpArgs);
        _ctpProcess.Start();

        var readyTask = Task.Run(() =>
        {
            while (_ctpProcess.StandardOutput.ReadLine() != "READY") { }
        });
        if (!readyTask.Wait(TimeSpan.FromSeconds(5)))
            throw new Exception("Did not receive READY before timeout");
    }

    public ExtractedFileStatus Anonymise(FileInfo sourceFile, FileInfo destFile, string modality, out string? anonymiserStatusMessage)
    {
        _logger.Info($"{sourceFile} to {destFile}");

        var args = $"{sourceFile.FullName} {destFile.FullName}";
        _ctpProcess.StandardInput.WriteLine(args);
        var result = _ctpProcess.StandardOutput.ReadLine();
        _logger.Info(result);

        ExtractedFileStatus status;
        if (result == "OK")
        {
            anonymiserStatusMessage = null;
            status = ExtractedFileStatus.Anonymised;
        }
        else
        {
            anonymiserStatusMessage = result;
            status = ExtractedFileStatus.ErrorWontRetry;
        }

        return status;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _ctpProcess.Dispose();
    }
}