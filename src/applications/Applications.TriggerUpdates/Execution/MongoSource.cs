using Smi.Common.Messages.Updating;
using Smi.Common.Options;

namespace TriggerUpdates.Execution
{
    public class MongoSource : ITriggerUpdatesSource
    {
        public MongoSource(GlobalOptions globalOptions,TriggerUpdatesFromMongo cliOptions)
        {
        }

        public UpdateValuesMessage Next()
        {
            throw new System.NotImplementedException();
        }
    }
}