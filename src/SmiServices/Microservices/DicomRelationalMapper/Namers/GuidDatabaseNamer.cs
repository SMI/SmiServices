using FAnsi.Discovery;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.EntityNaming;
using System;

namespace SmiServices.Microservices.DicomRelationalMapper.Namers
{
    /// <summary>
    /// Handles naming RAW/STAGING databases in a data load with unique names (by prefixing a GUID to the name).
    /// 
    /// Since the RDMP currently expects STAGING to always be there and won't automatically create it we have to also support creating
    /// it externally and destroying it after successful loads.
    /// </summary>
    public class GuidDatabaseNamer : FixedStagingDatabaseNamer, ICreateAndDestroyStagingDuringLoads
    {
        private readonly string _guid;
        private DiscoveredDatabase? _stagingDatabase;

        /// <summary>
        /// Defines how to name Staging databases by appending a Guid.  You can pass a specific guid if you want or pass Guid.Empty to 
        /// assign a new random one
        /// </summary>
        /// <param name="databaseName"></param>
        /// <param name="explicitGuid"></param>
        public GuidDatabaseNamer(string databaseName, Guid explicitGuid)
            : base(databaseName)
        {
            _guid = explicitGuid == Guid.Empty ? Guid.NewGuid().ToString("N") : explicitGuid.ToString();
        }

        /// <summary>
        /// Prefixes STAGING and RAW databases with the guid
        /// </summary>
        /// <param name="rootDatabaseName"></param>
        /// <param name="stage"></param>
        /// <returns></returns>
        public override string GetDatabaseName(string? rootDatabaseName, LoadBubble stage)
        {
            var basic = base.GetDatabaseName(rootDatabaseName, stage);

            if (stage == LoadBubble.Live || stage == LoadBubble.Archive)
                return basic;

            return "t" + _guid.Replace("-", "") + basic;
        }

        public void CreateStaging(DiscoveredServer liveServer)
        {
            _stagingDatabase = liveServer.ExpectDatabase(GetDatabaseName(null, LoadBubble.Staging));

            if (!_stagingDatabase.Exists())
                _stagingDatabase.Create();

            //get rid of any old data from previous load
            foreach (var t in _stagingDatabase.DiscoverTables(false))
                t.Truncate();
        }

        public void DestroyStagingIfExists()
        {
            if (_stagingDatabase != null && _stagingDatabase.Exists())
                _stagingDatabase.Drop();
        }

        public override string ToString()
        {
            return base.ToString() + "(GUID:" + _guid + ")";
        }
    }
}
