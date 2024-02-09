using System;
using System.IO;
using System.IO.Abstractions;
using System.Diagnostics;
using Smi.Common.Messages.Extraction;
using Newtonsoft.Json;
using Smi.Common.Options;

namespace Microservices.DicomAnonymiser.Anonymisers
{
    public class DefaultAnonymiser : IDicomAnonymiser
    {
        private readonly DicomAnonymiserOptions _options;

        public DefaultAnonymiser(DicomAnonymiserOptions dicomAnonymiserOptions)
        {
            _options = dicomAnonymiserOptions;
        }

        /// <summary>
        /// Creates a process to run the dicom pixel anonymiser
        /// </summary>
        /// <param name="sourceFile"></param>
        /// <param name="destFile"></param>
        /// <returns></returns>
        private Process CreateDICOMProcess(IFileInfo sourceFile, IFileInfo destFile)
        {
            string activateCommand = $"source {_options.VirtualEnvPath}/bin/activate";

            Process process = new Process();

            process.StartInfo.FileName = "/bin/bash";
            process.StartInfo.Arguments = $"-c \"{activateCommand} && {_options.ShellScriptPath} -o {destFile} {sourceFile}\"";
            process.StartInfo.WorkingDirectory = $"{_options.DicomPixelAnonPath}/src/applications/";
            process.StartInfo.EnvironmentVariables["SMI_ROOT"] = $"{_options.SmiServicesPath}";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            return process;
        }

        /// <summary>
        /// Creates a process to run the CTP Anonymiser
        /// </summary>
        private Process CreateCTPProcess(IFileInfo sourceFile, IFileInfo destFile)
        {
            Process process = new Process();

            process.StartInfo.FileName = "java";
            process.StartInfo.Arguments = $"-jar {_options.CtpJarPath} -a {_options.CtpWhiteListScriptPath} -s false {sourceFile} {destFile}";
            process.StartInfo.EnvironmentVariables["SMI_ROOT"] = $"{_options.SmiServicesPath}";
            process.StartInfo.EnvironmentVariables["SMI_LOGS_ROOT"] = $"{_options.SmiLogsPath}"; 
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            return process;
        }

        /// <summary>
        /// Creates a process to run the DICOM to Text conversion
        /// </summary>
        private Process CreateDICOMToTextProcess(IFileInfo sourceFile, IFileInfo destFile)
        {
            string pythonExe = System.IO.Path.Combine(_options.VirtualEnvPath!, "bin/python3");

            Process process = new Process();

            process.StartInfo.FileName = pythonExe;
            process.StartInfo.Arguments = $"{_options.DicomToTextScriptPath} -i {sourceFile} -o /Users/daniyalarshad/EPCC/github/NationalSafeHaven/CogStack-SemEHR/anonymisation/test_data/output.txt -y /Users/daniyalarshad/EPCC/github/NationalSafeHaven/SmiServices/data/microserviceConfigs/default.yaml";
            process.StartInfo.EnvironmentVariables["SMI_ROOT"] = $"{_options.SmiServicesPath}";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            return process;
        }

        /// <summary>
        /// Creates a process to run the SR Anonymiser
        /// </summary>
        private Process CreateSRProcess(IFileInfo sourceFile, IFileInfo destFile)
        {
            string pythonExe = System.IO.Path.Combine(_options.VirtualEnvPath!, "bin/python3");

            Process process = new Process();

            process.StartInfo.FileName = pythonExe;
            process.StartInfo.Arguments = $"{_options.AnonymiseSRScriptPath} conf/anonymisation_task.json";
            process.StartInfo.WorkingDirectory = "/Users/daniyalarshad/EPCC/github/NationalSafeHaven/CogStack-SemEHR/anonymisation/";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            return process;
        }

        /// <summary>
        ///  Anonymises a DICOM file based on the modality
        /// </summary>
        /// <param name="message"></param>
        /// <param name="sourceFile"></param>
        /// <param name="destFile"></param>
        /// <returns></returns>
        public ExtractedFileStatus Anonymise(ExtractFileMessage message, IFileInfo sourceFile, IFileInfo destFile)
        {
            Console.WriteLine($"INFO: Anonymising {sourceFile} to {destFile}");

            // CTP Anonymiser
            Process CTPProcess = CreateCTPProcess(sourceFile, destFile);
            CTPProcess.Start();
            CTPProcess.WaitForExit();

            string CTPReturnCode = CTPProcess.ExitCode.ToString();
            if (CTPReturnCode != "0")
            {
                Console.WriteLine($"ERROR [CTP]: Return Code {CTPReturnCode}");
                System.Console.WriteLine(CTPProcess.StandardError.ReadToEnd());
                return ExtractedFileStatus.ErrorWontRetry;
            }
            else
            {
                Console.WriteLine($"SUCCESS [CTP]: Return Code {CTPReturnCode}");
                System.Console.WriteLine(CTPProcess.StandardOutput.ReadToEnd());

                if(message.Modality == "SR")
                {
                    Process DICOMToTextProcess = CreateDICOMToTextProcess(sourceFile, destFile);
                    DICOMToTextProcess.Start();
                    DICOMToTextProcess.WaitForExit();

                    string DICOMToTextReturnCode = DICOMToTextProcess.ExitCode.ToString();
                    if (DICOMToTextReturnCode != "0")
                    {
                        Console.WriteLine($"ERROR [DICOMToText]: Return Code {DICOMToTextReturnCode}");
                        System.Console.WriteLine(DICOMToTextProcess.StandardError.ReadToEnd());
                        return ExtractedFileStatus.ErrorWontRetry;
                    }
                    else
                    {
                        Console.WriteLine($"SUCCESS [DICOMToText]: Return Code {DICOMToTextReturnCode}");
                        System.Console.WriteLine(DICOMToTextProcess.StandardOutput.ReadToEnd());

                        // SR Anonymiser
                        Process SRProcess = CreateSRProcess(sourceFile, destFile);
                        SRProcess.Start();
                        SRProcess.WaitForExit();

                        string SRReturnCode = SRProcess.ExitCode.ToString();
                        if (SRReturnCode != "0")
                        {
                            Console.WriteLine($"ERROR [SR]: Return Code {SRReturnCode}");
                            System.Console.WriteLine(SRProcess.StandardError.ReadToEnd());
                            return ExtractedFileStatus.ErrorWontRetry;
                        }
                        else
                        {
                            Console.WriteLine($"SUCCESS [SR]: Return Code {SRReturnCode}");
                            System.Console.WriteLine(SRProcess.StandardOutput.ReadToEnd());
                            return ExtractedFileStatus.Anonymised;
                        }
                    }
                }
                else
                {
                    // DICOM Pixel Anonymiser
                    Process DICOMProcess = CreateDICOMProcess(sourceFile, destFile);
                    DICOMProcess.Start();
                    DICOMProcess.WaitForExit();

                    string DICOMReturnCode = DICOMProcess.ExitCode.ToString();
                    if (DICOMReturnCode != "0")
                    {
                        Console.WriteLine($"ERROR [DICOM]: Return Code {DICOMReturnCode}");
                        System.Console.WriteLine(DICOMProcess.StandardError.ReadToEnd());
                        return ExtractedFileStatus.ErrorWontRetry;
                    }
                    else
                    {
                        Console.WriteLine($"SUCCESS [DICOM]: Return Code {DICOMReturnCode}");
                        System.Console.WriteLine(DICOMProcess.StandardOutput.ReadToEnd());
                        return ExtractedFileStatus.Anonymised;
                    }
                }
            }
        }
    }
}
