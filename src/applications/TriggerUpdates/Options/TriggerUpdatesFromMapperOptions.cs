using CommandLine;

namespace TriggerUpdates
{
    [Verb("mapper", HelpText = "Triggers updates based on new identifier mapping table updates")]
    public class TriggerUpdatesFromMapperOptions : TriggerUpdatesOptions
    {
    }
}
