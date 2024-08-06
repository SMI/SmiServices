using FAnsi.Discovery;
using Rdmp.Core.Curation.Data.EntityNaming;

namespace SmiServices.Microservices.DicomRelationalMapper.Namers
{
    /// <summary>
    /// interface for <see cref="INameDatabasesAndTablesDuringLoads"/> implementations which can on demand create the STAGING database
    /// (which must be on the same server as LIVE).
    /// </summary>
    public interface ICreateAndDestroyStagingDuringLoads : INameDatabasesAndTablesDuringLoads
    {
        void CreateStaging(DiscoveredServer liveServer);
        void DestroyStagingIfExists();
    }
}
