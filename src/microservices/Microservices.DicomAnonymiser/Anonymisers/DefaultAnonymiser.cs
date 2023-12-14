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

        public DefaultAnonymiser()
        {
            _virtualEnvPath = "";
            _shellScriptPath = "";
            _dicomPixelAnonPath = "";
            _smiServicesPath = "";

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

        // TODO (da-231122) - Update CreateProcess logic based on image modality
        /// <summary>
        /// Creates a process to run the dicom pixel anonymiser
        /// </summary>
        /// <param name="sourceFile"></param>
        /// <param name="destFile"></param>
        /// <returns></returns>
        private Process CreateProcess(IFileInfo sourceFile, IFileInfo destFile)
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

        // TODO (da-231122) - Update Anonymise logic based on image modality
        /// <summary>
        ///  Anonymises a DICOM file using the dicom pixel anonymiser
        /// </summary>
        /// <param name="message"></param>
        /// <param name="sourceFile"></param>
        /// <param name="destFile"></param>
        /// <returns></returns>
        public ExtractedFileStatus Anonymise(ExtractFileMessage message, IFileInfo sourceFile, IFileInfo destFile)
        {
            Console.WriteLine($"INFO: Anonymising {sourceFile} to {destFile}");

            Process process = CreateProcess(sourceFile, destFile);
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            if (error != "")
            {
                Console.WriteLine($"ERROR: {error}");
                return ExtractedFileStatus.ErrorWontRetry;
            } else
            {
                Console.WriteLine($"INFO: Output: {output}");
            }

            process.WaitForExit();

            string returnCode = process.ExitCode.ToString();
            Console.WriteLine($"INFO: Return Code: {returnCode}");

            if (returnCode != "0")
            {
                return ExtractedFileStatus.ErrorWontRetry;
            }

            return ExtractedFileStatus.Anonymised;
        }
    }
}