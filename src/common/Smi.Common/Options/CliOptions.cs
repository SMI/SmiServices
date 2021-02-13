﻿using CommandLine;

namespace Smi.Common.Options
{
    public class CliOptions
    {
        [Option(
            'y',
            "yaml-file",
            Default = "default.yaml",
            Required = false,
            HelpText = "[Optional] Name of the yaml config file to load"
        )]
        public string YamlFile { get; set; }
    }
}
