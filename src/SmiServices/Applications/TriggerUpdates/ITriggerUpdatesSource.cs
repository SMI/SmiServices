using SmiServices.Common.Messages.Updating;
using System.Collections.Generic;


namespace SmiServices.Applications.TriggerUpdates;

public interface ITriggerUpdatesSource
{
    /// <summary>
    /// Returns updates to issue if any
    /// </summary>
    /// <returns></returns>
    IEnumerable<UpdateValuesMessage> GetUpdates();

    /// <summary>
    /// Notifies the source that it should cancel any ongoing queries and attempt to stop issuing updates
    /// </summary>
    void Stop();
}
