using CommandLine;
using System;

namespace TriggerUpdates
{
    [Verb("mapper", HelpText = "Triggers updates based on new identifier mapping table updates")]
    public class TriggerUpdatesFromMapperOptions : TriggerUpdatesCliOptions
    {
        [Option('d',"DateOfLastUpdate",Required = true,HelpText = "The last known date where live tables and mapping table were in sync.  Updates will be issued for records changed after this date")]
        public DateTime DateOfLastUpdate { get; set; }
    }
}
