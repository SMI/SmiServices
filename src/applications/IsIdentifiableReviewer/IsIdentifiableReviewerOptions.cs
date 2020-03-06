using CommandLine;
using IsIdentifiableReviewer.Out;

namespace IsIdentifiableReviewer
{
    public class IsIdentifiableReviewerOptions
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
    }
}