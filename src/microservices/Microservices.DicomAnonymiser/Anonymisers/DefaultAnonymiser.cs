using System;
using System.IO;
using System.IO.Abstractions;
using System.Diagnostics;
using Smi.Common.Messages.Extraction;
using Newtonsoft.Json;

namespace Microservices.DicomAnonymiser.Anonymisers
{
    public class DefaultAnonymiser : IDicomAnonymiser
    {
        private string _virtualEnvPath;
        private string _shellScriptPath;
        private string _dicomPixelAnonPath;
        private string _smiServicesPath;
        private string _ctpJarPath;
        private string _ctpWhiteListScriptPath;
        private string _srAnonToolPath;
        private string _smiLogsPath;
        private string _dicomToTextScriptPath;
        private string _anonymiseSRScriptPath;



        public DefaultAnonymiser()
        {
            _virtualEnvPath = "";
            _shellScriptPath = "";
            _dicomPixelAnonPath = "";
            _smiServicesPath = "";
            _ctpJarPath = "";
            _ctpWhiteListScriptPath = "";
            _srAnonToolPath = "";
            _smiLogsPath = "";
            _dicomToTextScriptPath = "";
            _anonymiseSRScriptPath = "";

            LoadConfiguration();
        }

        /// <summary>
        /// Loads configuration from DicomAnonymiserConfigs.json
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        private void LoadConfiguration()
        {
            // TODO (da-231122) - Load dicomAnonymiserConfigPath from default.yaml
            string dicomAnonymiserConfigPath  = "/Users/daniyalarshad/EPCC/github/SmiServices/src/microservices/Microservices.DicomAnonymiser/Anonymisers/DicomAnonymiserConfigs.json";

            if (File.Exists(dicomAnonymiserConfigPath))
            {
                string json = File.ReadAllText(dicomAnonymiserConfigPath);
                dynamic? config = JsonConvert.DeserializeObject<dynamic>(json);

                if (config != null){
                    _virtualEnvPath = config.virtualEnvPath;
                    _shellScriptPath = config.shellScriptPath;
                    _dicomPixelAnonPath = config.dicomPixelAnonPath;
                    _smiServicesPath = config.smiServicesPath;
                    _ctpJarPath = config.ctpJarPath;
                    _ctpWhiteListScriptPath = config.ctpWhiteListScriptPath;
                    _srAnonToolPath = config.srAnonToolPath;
                    _smiLogsPath = config.smiLogsPath;
                    _dicomToTextScriptPath = config.dicomToTextScriptPath;
                    _anonymiseSRScriptPath = config.anonymiseSRScriptPath;
                    }
                else
                {
                    Console.WriteLine("ERROR: Unable to deserialize configuration file 'DicomAnonymiserConfigs.json'");
                    throw new InvalidOperationException("Unable to deserialize configuration file 'DicomAnonymiserConfigs.json'");
                }
            }
            else
            {
                Console.WriteLine("ERROR: Configuration file 'DicomAnonymiserConfigs.json' not found");
                throw new FileNotFoundException("Configuration file 'DicomAnonymiserConfigs.json' not found");
            }
        }

        /// <summary>
        /// Creates a process to run the dicom pixel anonymiser
        /// </summary>
        /// <param name="sourceFile"></param>
        /// <param name="destFile"></param>
        /// <returns></returns>
        private Process CreateDICOMProcess(IFileInfo sourceFile, IFileInfo destFile)
        {
            string activateCommand = $"source {_virtualEnvPath}/bin/activate";

            Process process = new Process();

            process.StartInfo.FileName = "/bin/bash";
            process.StartInfo.Arguments = $"-c \"{activateCommand} && {_shellScriptPath} -o {destFile} {sourceFile}\"";
            process.StartInfo.WorkingDirectory = $"{_dicomPixelAnonPath}/src/applications/";
            process.StartInfo.EnvironmentVariables["SMI_ROOT"] = $"{_smiServicesPath}";
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
            process.StartInfo.Arguments = $"-jar {_ctpJarPath} -a {_ctpWhiteListScriptPath} -s false {sourceFile} {destFile}";
            process.StartInfo.EnvironmentVariables["SMI_ROOT"] = $"{_smiServicesPath}";
            process.StartInfo.EnvironmentVariables["SMI_LOGS_ROOT"] = $"{_smiLogsPath}"; 
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
            string pythonExe = System.IO.Path.Combine(_virtualEnvPath, "bin/python3");

            Process process = new Process();

            process.StartInfo.FileName = pythonExe;
            process.StartInfo.Arguments = $"{_dicomToTextScriptPath} -i {sourceFile} -o /Users/daniyalarshad/EPCC/github/NationalSafeHaven/CogStack-SemEHR/anonymisation/test_data/output.txt -y /Users/daniyalarshad/EPCC/github/NationalSafeHaven/SmiServices/data/microserviceConfigs/default.yaml";
            process.StartInfo.EnvironmentVariables["SMI_ROOT"] = $"{_smiServicesPath}";
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
            string pythonExe = System.IO.Path.Combine(_virtualEnvPath, "bin/python3");

            Process process = new Process();

            process.StartInfo.FileName = pythonExe;
            process.StartInfo.Arguments = $"{_anonymiseSRScriptPath} conf/anonymisation_task.json";
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
