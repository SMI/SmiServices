using Smi.Common.Messages.Updating;

namespace TriggerUpdates.Execution
{
    public class MapperSource : ITriggerUpdatesSource
    {
        private TriggerUpdatesFromMapperOptions opts;

        public MapperSource(TriggerUpdatesFromMapperOptions opts)
        {
            this.opts = opts;
        }

        public UpdateValuesMessage Next()
        {
            throw new System.NotImplementedException();
        }
    }
}