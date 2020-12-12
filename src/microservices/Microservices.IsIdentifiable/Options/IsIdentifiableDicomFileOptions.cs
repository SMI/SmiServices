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

        [Option(HelpText = "Optional. Path to a 'tessdata' directory.  tessdata.eng will be created here (unless a more recent version already exists).  If specified then the DICOM file's pixel data will be run through text detection")]
        public string TessDirectory { get; set; }

        [Option(HelpText = "Optional. If set images will be rotated to 90, 180 and 270 degrees (clockwise) to allow OCR to pick up upside down or horizontal text.")]
        public bool Rotate { get; set; }

        [Option(HelpText = "Optional.  If set any image tag which contains a DateTime will result in a failure")]
        public bool NoDateFields { get; set; }

        [Option(HelpText = "Optional.  If NoDateFields is set then this value will not result in a failure.  e.g. 0001-01-01")]
        public string ZeroDate { get; set; }

        [Option(HelpText = "Optional. If non-zero, will ignore any reported pixel data text less than (but not equal to) the specified number of characters")]
        public uint IgnoreTextLessThan { get; set; } = 0;

        [Usage]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Classify all *.dcm files and run the pixel text classifier.  This example works if the 'tessdata' is in the current directory",
                    new IsIdentifiableDicomFileOptions
                    {
                        Directory = @"C:\MyDataFolder",
                        TessDirectory = ".",
                        StoreReport = true
                    });

                yield return new Example("Attempt to interpret all files as DICOM",
                    new IsIdentifiableDicomFileOptions
                    {
                        Directory = @"C:\MyDataFolder",
                        Pattern = "*",
                            StoreReport = true
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

            if (string.IsNullOrWhiteSpace(TessDirectory) && Rotate)
                throw new Exception("Rotate option is only valid if OCR is running (TessDirectory is set)");

            if (!string.IsNullOrWhiteSpace(ZeroDate) && !NoDateFields)
                throw new Exception("ZeroDate is only valid if the NoDateFields flag is set");
        }
    }
}