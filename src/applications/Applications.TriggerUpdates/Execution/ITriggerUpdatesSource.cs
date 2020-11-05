using Smi.Common.Messages.Updating;

namespace TriggerUpdates.Execution
{
    public interface ITriggerUpdatesSource
    {
        /// <summary>
        /// Returns the next update to issue or null if there are no more updates to issue
        /// </summary>
        /// <returns></returns>
        UpdateValuesMessage Next();
    }
}