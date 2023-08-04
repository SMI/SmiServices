
using CommandLine;
using CommandLine.Text;
using ReusableLibraryCode.Annotations;
using Smi.Common.Options;
using System.Collections.Generic;
using System.IO;

namespace Applications.DicomDirectoryProcessor.Options
{
    public class DicomDirectoryProcessorCliOptions : CliOptions
    {
        [UsedImplicitly]
        [Option('d', "to-process", Required = true, HelpText = "The directory to process")]
        public string ToProcess { get; set; } = null!;

        [UsedImplicitly]
        [Option('f', "directory-format", Required = false, HelpText = "The specific directory search format to use (case insensitive).  Options include PACS,LIST,ZIPS and DEFAULT", Default = "Default")]
        public string? DirectoryFormat { get; set; }


        public DirectoryInfo ToProcessDir
        {
            get
            {
                return ToProcess == null
                    ? null
                    : new DirectoryInfo(ToProcess);
            }
            set => ToProcess = value?.FullName;
        }

        [Usage]
        [UsedImplicitly]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return
                    new Example("Normal Scenario", new DicomDirectoryProcessorCliOptions { ToProcess = @"c:\temp\bob" });
                yield return
                    new Example("Override Yaml File", new DicomDirectoryProcessorCliOptions { ToProcess = @"c:\temp\bob", YamlFile = "myconfig.yaml" });
                yield return
                    new Example("Search using the PACS directory structure", new DicomDirectoryProcessorCliOptions { ToProcess = @"c:\temp\bob", DirectoryFormat = "PACS" });
            }
        }

        public override string ToString()
        {
            return base.ToString() + "ToProcess: \"" + ToProcess + ", DirectoryFormat" + DirectoryFormat + "\"\n";
        }
    }
}
