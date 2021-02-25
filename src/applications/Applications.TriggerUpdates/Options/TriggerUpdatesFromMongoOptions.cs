using CommandLine;


namespace Applications.TriggerUpdates.Options
{
    [Verb("mongo", HelpText = "Triggers updates for a specific tag in ")]
    public class TriggerUpdatesFromMongoOptions : TriggerUpdatesCliOptions
    {
    }
}
