using Smi.Common.Messages.Updating;
using Smi.Common.Options;
using System.Collections.Generic;

namespace TriggerUpdates.Execution
{
    public class MongoSource : ITriggerUpdatesSource
    {
        public MongoSource(GlobalOptions globalOptions,TriggerUpdatesFromMongo cliOptions)
        {
        }

        public IEnumerable<UpdateValuesMessage> GetUpdates()
        {
            throw new System.NotImplementedException();
        }
    }
}