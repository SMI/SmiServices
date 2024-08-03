using System;
using System.IO;
using System.IO.Abstractions;
using System.Diagnostics;
using System.Collections.Generic;
using Smi.Common.Messages.Extraction;
using Smi.Common.Options;
using Newtonsoft.Json;
using NLog;

namespace Microservices.DicomAnonymiser.Anonymisers
{
    public class DefaultAnonymiser : IDicomAnonymiser
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly DicomAnonymiserOptions _options;
        private readonly LoggingOptions _loggingOptions;
        private const string _bash = "/bin/bash";

        public DefaultAnonymiser(GlobalOptions globalOptions)
        {
            if (globalOptions.DicomAnonymiserOptions == null)
                throw new ArgumentNullException(nameof(globalOptions.DicomAnonymiserOptions));

            if (globalOptions.LoggingOptions == null)
                throw new ArgumentNullException(nameof(globalOptions.LoggingOptions));

            _options = globalOptions.DicomAnonymiserOptions;

            // TODO (da 2024-02-23) Review if LoggingOptions is needed
            _loggingOptions = globalOptions.LoggingOptions;
        }

        /// <summary>
        /// Creates a process with the given parameters
        /// </summary>
        private Process CreateProcess(string fileName, string arguments, string? workingDirectory = null, Dictionary<string, string>? environmentVariables = null)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = workingDirectory ?? string.Empty
                }
            };

            if (environmentVariables != null)
            {
                foreach (var variable in environmentVariables)
                {
                    process.StartInfo.EnvironmentVariables[variable.Key] = variable.Value;
                }
            }

            return process;
        }

        private Process CreateCTPAnonProcess(IFileInfo sourceFile, IFileInfo destFile)
        {
            string arguments = $"-jar {_options.CtpAnonCliJar} -a {_options.CtpAllowlistScript} -s false {sourceFile} {destFile}";

            return CreateProcess("java", arguments);
        }

        private Process CreatePixelAnonProcess(IFileInfo sourceFile, IFileInfo destFile)
        {
            string activateCommand = $"source {_options.VirtualEnvPath}/bin/activate";
            string arguments = $"-c \"{activateCommand} && {_options.DicomPixelAnonPath}/dicom_pixel_anon.sh -o {destFile} {sourceFile}\"";

            return CreateProcess(_bash, arguments, _options.DicomPixelAnonPath);
        }

        // TODO (da 2024-02-23) Use StructuredReports repository to access SRAnonTool
        private Process CreateSRAnonProcess(IFileInfo sourceFile, IFileInfo destFile)
        {
            string arguments = $"{_options.SRAnonymiserToolPath} -i {sourceFile} -o {destFile} -s /Users/daniyalarshad/EPCC/github/NationalSafeHaven/opt/semehr";
            var environmentVariables = new Dictionary<string, string> { { "SMI_ROOT", $"{_options.SmiServicesPath}" } };

            return CreateProcess(_bash, arguments);
        }

        /// <summary>
        /// Anonymises a DICOM file based on image modality
        /// </summary>
        public ExtractedFileStatus Anonymise(ExtractFileMessage message, IFileInfo sourceFile, IFileInfo destFile, out string anonymiserStatusMessage)
        {
            _logger.Info($"Anonymising {sourceFile} to {destFile}");

            if (!RunProcessAndCheckSuccess(CreateCTPAnonProcess(sourceFile, destFile), "CTP Anonymiser"))
            {
                anonymiserStatusMessage = "Error running CTP anonymiser";
                return ExtractedFileStatus.ErrorWontRetry;
            }

            if (message.Modality == "SR")
            {
                if (!RunProcessAndCheckSuccess(CreateSRAnonProcess(sourceFile, destFile), "SR Anonymiser"))
                {
                    anonymiserStatusMessage = "Error running SR anonymiser";
                    return ExtractedFileStatus.ErrorWontRetry;
                }
            }
            else
            {
                if (!RunProcessAndCheckSuccess(CreatePixelAnonProcess(sourceFile, destFile), "Pixel Anonymiser"))
                {
                    anonymiserStatusMessage = "Error running PIXEL anonymiser";
                    return ExtractedFileStatus.ErrorWontRetry;
                }
            }

            anonymiserStatusMessage = "Anonymisation successful";
            return ExtractedFileStatus.Anonymised;
        }

        /// <summary>
        /// Runs a process and logs the result
        /// </summary>
        private bool RunProcessAndCheckSuccess(Process process, string processName)
        {
            process.Start();
            process.WaitForExit();

            var returnCode = process.ExitCode.ToString();
            LogProcessResult(processName, returnCode, process);

            return returnCode == "0";
        }

        /// <summary>
        /// Logs the result of a process
        /// </summary>
        private void LogProcessResult(string processName, string returnCode, Process process)
        {
            var output = returnCode == "0" ? process.StandardOutput.ReadToEnd() : process.StandardError.ReadToEnd();
            _logger.Info($"{(returnCode == "0" ? "SUCCESS" : "ERROR")} [{processName}]: Return Code {returnCode}\n{output}");
        }

    }
}
