using System.Collections.Generic;
using Applications.TriggerUpdates.Options;
using Smi.Common.Messages.Updating;
using Smi.Common.Options;


namespace Applications.TriggerUpdates.Execution
{
    public class MongoSource : ITriggerUpdatesSource
    {
        private TriggerUpdatesFromMongoOptions _cliOptions;

        public MongoSource(GlobalOptions globalOptions,TriggerUpdatesFromMongoOptions cliOptions)
        {
            _cliOptions = cliOptions;
        }

        public IEnumerable<UpdateValuesMessage> GetUpdates()
        {
            throw new System.NotImplementedException();
        }

        public void Stop()
        {
            throw new System.NotImplementedException();
        }
    }
}
