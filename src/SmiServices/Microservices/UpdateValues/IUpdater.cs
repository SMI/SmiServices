using SmiServices.Common.Messages.Updating;

namespace SmiServices.Microservices.UpdateValues;

public interface IUpdater
{
    /// <summary>
    /// Update one or more database tables to fully propagate <paramref name="message"/> to all relevant tables
    /// </summary>
    /// <param name="message">What should be updated</param>
    /// <returns>total number of rows updated in the database(s)</returns>
    int HandleUpdate(UpdateValuesMessage message);
}
