using Smi.Common.Messages.Updating;

namespace TriggerUpdates.Execution
{
    internal class MongoSource : ITriggerUpdatesSource
    {
        private TriggerUpdatesFromMongo opts;

        public MongoSource(TriggerUpdatesFromMongo opts)
        {
            this.opts = opts;
        }

        public UpdateValuesMessage Next()
        {
            throw new System.NotImplementedException();
        }
    }
}