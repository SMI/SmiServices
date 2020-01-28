using System;
using System.Collections.Generic;
using System.IO;
using CommandLine;
using CommandLine.Text;

namespace Microservices.IsIdentifiable.Options
{
    [Verb("dir")]
    public class IsIdentifiableDicomFileOptions : IsIdentifiableAbstractOptions
    {
        [Option('d', HelpText = "Directory in which to recursively search for dicom files", Required = true)]
        public string Directory { get; set; }

        [Option(HelpText = "Optional. Search pattern for files, defaults to *.dcm)", Default = "*.dcm")]
        public string Pattern{ get; set; }
        
        [Option(HelpText = "Optional. True to check the files opened have a valid dicom preamble", Default = true)]
        public bool RequirePreamble { get; set; }

        [Option(HelpText = "Optional. Pass a hostname with port (e.g. localhost:2020) to an OCR classifier that will be passed file paths for classification")]
        public string OCRHost { get; set; }

        [Option(HelpText = "Optional.  If set any image tag which contains a DateTime will result in a failure")]
        public bool NoDateFields { get; set; }

        [Option(HelpText = "Optional.  If NoDateFields is set then this value will not result in a failure.  e.g. 0001-01-01")]
        public string ZeroDate { get; set; }

        [Usage]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Classify all *.dcm files and run the pixel text classifier.  This example works if the 'tessdata' is in the current directory",
                    new IsIdentifiableDicomFileOptions
                    {
                        Directory = @"C:\MyDataFolder",
                        StoreReport = true
                    });

                yield return new Example("Attempt to interpret all files as DICOM",
                    new IsIdentifiableDicomFileOptions
                    {
                        Directory = @"C:\MyDataFolder",
                        Pattern = "*"
                    });
            }
        }

        public override string GetTargetName()
        {
            return Directory == null ?"No Directory Specified":new DirectoryInfo(Directory).Name;
        }

        public override void ValidateOptions()
        {
            base.ValidateOptions();

            if (!string.IsNullOrWhiteSpace(ZeroDate) && !NoDateFields)
                throw new Exception("ZeroDate is only valid if the NoDateFields flag is set");
        }
    }
}
