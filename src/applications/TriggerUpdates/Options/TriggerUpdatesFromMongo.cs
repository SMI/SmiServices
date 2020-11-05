using CommandLine;

namespace TriggerUpdates
{
    [Verb("mongo", HelpText = "Triggers updates for a specific tag in ")]
    public class TriggerUpdatesFromMongo : TriggerUpdatesOptions
    {
    }
}
