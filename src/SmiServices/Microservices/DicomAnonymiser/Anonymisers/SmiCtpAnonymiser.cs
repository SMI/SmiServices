using NLog;
using SmiServices.Common.Messages.Extraction;
using SmiServices.Common.Options;
using SmiServices.Microservices.DicomAnonymiser.Helpers;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace SmiServices.Microservices.DicomAnonymiser.Anonymisers;

public class SmiCtpAnonymiser : IDicomAnonymiser, IDisposable
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
        _logger.Info($"Starting ctp with: java {ctpArgs}");
        _ctpProcess = ProcessWrapper.CreateProcess("java", ctpArgs);
        var ready = false;
        _ctpProcess.OutputDataReceived += (process, args) =>
        {
            _logger.Debug(args.Data);
            if ("READY" != args.Data) return;

            ready = true;
            lock (_ctpProcess)
                Monitor.Pulse(_ctpProcess);
        };
        _ctpProcess.ErrorDataReceived += (process, args) => _logger.Debug(args.Data);
        _ctpProcess.Start();
        _ctpProcess.BeginOutputReadLine();
        _ctpProcess.BeginErrorReadLine();

        lock (_ctpProcess)
            Monitor.Wait(_ctpProcess, 100000);
        if (ready) return;

        _ctpProcess.Kill();
        throw new Exception($"Did not receive READY before timeout");
    }

    public ExtractedFileStatus Anonymise(IFileInfo sourceFile, IFileInfo destFile, string modality, out string? anonymiserStatusMessage)
    {
        var args = $"{sourceFile.FullName} {destFile.FullName}";
        _logger.Debug($"> {args}");
        _ctpProcess.StandardInput.WriteLine(args);

        var result = _ctpProcess.StandardOutput.ReadLine();
        _logger.Info($"< {result}");

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
