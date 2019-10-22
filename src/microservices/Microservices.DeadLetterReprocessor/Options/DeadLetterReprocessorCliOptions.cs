
using CommandLine;
using Smi.Common.Options;

namespace Microservices.DeadLetterReprocessor.Options
{
    public class DeadLetterReprocessorCliOptions : CliOptions
    {
        [Option('f', "flush-messages", Default = false, HelpText = "Reprocess all messages regardless of their timeouts")]
        public bool FlushMessages { get; set; }

        [Option('q', "reprocess-queue", Required = false, HelpText = "If set, will only reprocess messages from the given queue")]
        public string ReprocessFromQueue { get; set; }

        [Option('s', "store-only", Default = false, HelpText = "Only perform the storage operation, no messages will be republished")]
        public bool StoreOnly { get; set; }


        public override string ToString()
        {
            return "ReprocessFromQueue: " + ReprocessFromQueue +
                   ", FlushMessages: " + FlushMessages +
                   ", StoreOnly: " + StoreOnly;
        }
    }
}
