using CommandLine;

namespace IsIdentifiableReviewer
{
    class IsIdentifiableReviewerOptions
    {

        [Option('f', "file",
            Required = false,
            HelpText = "[Optional] Pre load an existing failures file"
        )]
        public string FailuresCsv { get; set; }
    }
}