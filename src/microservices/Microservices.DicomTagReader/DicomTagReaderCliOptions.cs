using CommandLine;
using Smi.Common.Options;
using System.IO;

namespace Microservices.DicomTagReader
{
    public class DicomTagReaderCliOptions : CliOptions
    {
        /// <summary>
        /// When not null this is the single file that should be considered instead of subscribing to RabbitMQ input queue
        /// </summary>
        [Option(
            'f',
            "file",
            Required = false,
            HelpText = "[Optional] Name of a specific dicom or zip file to process instead of subscribing to rabbit"
        )]
        public FileInfo? File { get; set; }
    }
}
