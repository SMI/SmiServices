using CommandLine;
using IsIdentifiableReviewer.Out;
using Smi.Common.Options;
using System.IO;

namespace IsIdentifiableReviewer
{
    /// <summary>
    /// CLI options for the reviewer
    /// </summary>
    public class IsIdentifiableReviewerOptions : CliOptions
    {
        [Option('f', "file",
            Required = false,
            HelpText = "[Optional] Pre load an existing failures file"
        )]
        public string FailuresCsv { get; set; }

        [Option('u', "unattended",
            Required = false,
            HelpText = "[Optional] Runs the application automatically processing existing update/ignore rules.  Failures not matching either are written to a new file with this path"
        )]
        public string UnattendedOutputPath { get; set; }

        [Option('t', "targets",
            Required = false,
            Default = "Targets.yaml",
            HelpText = "Location of database connection strings file (for issuing UPDATE statements)"
        )]
        public string TargetsFile { get; set; }

        [Option('i', "ignore",
            Required = false,
            Default = IgnoreRuleGenerator.DefaultFileName,
            HelpText = "File containing rules for ignoring validation errors"
        )]
        public string IgnoreList { get; set; }
        
        [Option('r', "redlist",
            Required = false,
            Default = RowUpdater.DefaultFileName,
            HelpText = "File containing rules for when to issue UPDATE statements"
        )]
        public string RedList { get; set; }


        [Option('o', "only-rules",
            Required = false,
            Default = false,
            HelpText = "Specify to make GUI UPDATE choices only create new rules instead of going to database"
        )]
        public bool OnlyRules { get; set; }

        /// <summary>
        /// Sets UseSystemConsole to true for Terminal.gui (i.e. uses the NetDriver which is based on System.Console)
        /// </summary>
        [Option("usc",HelpText = "Sets UseSystemConsole to true for Terminal.gui (i.e. uses the NetDriver which is based on System.Console)")]
        public bool UseSystemConsole { get; internal set; }

        /// <summary>
        /// Sets the user interface to use a specific color pallette yaml file
        /// </summary>
        [Option("theme", HelpText = "Sets the user interface to use a specific color pallette yaml file")]
        public FileInfo Theme { get; internal set; }


    }
}