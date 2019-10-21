
using CommandLine;

namespace Microservices.Common.Options
{
    public class CliOptions
    {
        [Option('y', "yaml-file", Default = "default.yaml", HelpText = "Name of the yaml config file to load")]
        public string YamlFile { get; set; }

        [Option("trace-logging", Default = true, HelpText = "Enable trace logging")]
        public bool TraceLogging { get; set; }
    }
}
