using Smi.Common.Messages.Updating;
using System.Collections.Generic;

namespace TriggerUpdates.Execution
{
    public interface ITriggerUpdatesSource
    {
        /// <summary>
        /// Returns updates to issue if any
        /// </summary>
        /// <returns></returns>
        IEnumerable<UpdateValuesMessage> GetUpdates();
    }
}