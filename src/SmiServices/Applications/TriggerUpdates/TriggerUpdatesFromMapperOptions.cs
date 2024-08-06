using CommandLine;
using System;


namespace SmiServices.Applications.TriggerUpdates
{
    [Verb("mapper", HelpText = "Triggers updates based on new identifier mapping table updates")]
    public class TriggerUpdatesFromMapperOptions : TriggerUpdatesCliOptions
    {
        [Option('d', "DateOfLastUpdate", Required = true, HelpText = "The last known date where live tables and mapping table were in sync.  Updates will be issued for records changed after this date")]
        public DateTime DateOfLastUpdate { get; set; }

        [Option('f', "FieldName", HelpText = "The field name of the release identifier in your databases e.g. PatientID.  Only needed if different from the mapping table swap column name e.g. ECHI")]
        public string? LiveDatabaseFieldName { get; set; }


        [Option('q', "Qualifier", HelpText = "Qualifier for values e.g. '.  This should be the DBMS qualifier needed for strings/dates.  If patient identifiers are numerical then do not specify this option")]
        public char? Qualifier { get; set; }
    }
}
