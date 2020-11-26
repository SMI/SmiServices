using System;
using System.Collections.Generic;
using System.IO;
using CommandLine;
using CommandLine.Text;

namespace Microservices.IsIdentifiable.Options
{
    [Verb("service")]
    public class IsIdentifiableServiceOptions : IsIdentifiableAbstractOptions
    {
        [Option('y', HelpText = "Configuration file", Required = true)]
        public string YamlFile { get; set; }
        
        [Option(HelpText = "Optional. True to check the files opened have a valid dicom preamble", Default = true)]
        public bool RequirePreamble { get; set; }

        [Option(HelpText = "Optional. If set images will be rotated to 90, 180 and 270 degrees (clockwise) to allow OCR to pick up upside down or horizontal text.")]
        public bool Rotate { get; set; }

        [Option(HelpText = "Optional.  If set any image tag which contains a DateTime will result in a failure")]
        public bool NoDateFields { get; set; }

        [Option(HelpText = "Optional.  If NoDateFields is set then this value will not result in a failure.  e.g. 0001-01-01")]
        public string ZeroDate { get; set; }

        [Option(HelpText = "Optional. If non-zero, will ignore any reported pixel data text less than (but not equal to) the specified number of characters")]
        public uint IgnoreTextLessThan { get; set; } = 0;

        public override string GetTargetName()
        {
            return "Service";
        }

        public override void ValidateOptions()
        {
            base.ValidateOptions();

            if (!string.IsNullOrWhiteSpace(ZeroDate) && !NoDateFields)
                throw new Exception("ZeroDate is only valid if the NoDateFields flag is set");
        }
    }
}