
using Applications.DicomRepopulator.Execution;
using Applications.DicomRepopulator.Options;
using CommandLine;
using System;

namespace Applications.DicomRepopulator
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<DicomRepopulatorOptions>(args).MapResult(
                dicomRepopulatorOptions =>
                {
                    if (dicomRepopulatorOptions.Validate())
                    {
                        var processor = new DicomRepopulatorProcessor();
                        return processor.Process(dicomRepopulatorOptions);
                    }

                    Console.Error.WriteLine("Options failed validation");
                    return -2;
                },
                errs => -1
                );
        }
    }
}
