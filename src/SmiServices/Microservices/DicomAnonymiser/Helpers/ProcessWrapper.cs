using System.Collections.Generic;
using System.Diagnostics;

namespace SmiServices.Microservices.DicomAnonymiser.Helpers
{
    internal static class ProcessWrapper
    {
        public static Process CreateProcess(
            string fileName,
            string? arguments,
            string? workingDirectory = null,
            Dictionary<string, string>? environmentVariables = null
        )
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = workingDirectory ?? string.Empty
                }
            };

            if (environmentVariables != null)
                foreach (var variable in environmentVariables)
                    process.StartInfo.EnvironmentVariables[variable.Key] = variable.Value;

            return process;
        }
    }
}
