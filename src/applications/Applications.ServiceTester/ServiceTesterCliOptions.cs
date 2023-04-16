using CommandLine;
using JetBrains.Annotations;
using Smi.Common.Options;

namespace Applications.ServiceTester
{
    internal class ServiceTesterCliOptions : CliOptions
    {
        [UsedImplicitly]
        [Option(shortName: 'f', longName: "message-file-path", Required = false, HelpText = "The file containing the message to publish")]
        public string MessageFilePath { get; set; }

        [UsedImplicitly]
        [Option(shortName: 'e', longName: "exchange-name", Required = false, HelpText = "The exchange to publish to")]
        public string ExchangeName { get; set; }

        [UsedImplicitly]
        [Option(shortName: 'r', longName: "routing-key", Required = false, HelpText = "The routing key to publish with")]
        public string RoutingKey { get; set; }

        [UsedImplicitly]
        [Option(shortName: 'p', longName: "print-message-template", Required = false, HelpText = "Prints the specified message template JSON and exits")]
        public string PrintMessageTemplate { get; set; }
    }
}
