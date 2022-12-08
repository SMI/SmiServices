using System;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.EntityNaming;

namespace Microservices.DicomRelationalMapper.Execution.Namers
{
    /// <summary>
    /// Handles naming RAW/STAGING databases in a data load with unique names.
    /// 
    /// Database names stay the same but table names get the prefix
    /// </summary>
    public class GuidTableNamer : FixedStagingDatabaseNamer
    {
        private readonly string _guid;

        /// <summary>
        /// Defines how to name RAW and Staging tables by appending a Guid.  You can pass a specific guid if you want or pass Guid.Empty to 
        /// assign a new random one
        /// </summary>
        /// <param name="databaseName"></param>
        /// <param name="explicitGuid"></param>
        public GuidTableNamer(string databaseName, Guid explicitGuid): base(databaseName)
        {
            _guid = explicitGuid == Guid.Empty ? Guid.NewGuid().ToString("N") : explicitGuid.ToString();

            //MySql can't handle long table names
            _guid = _guid.Substring(0, 8);
        }

        public override string GetName(string tableName, LoadBubble convention)
        {

            var basic = base.GetName(tableName, convention);

            if (convention == LoadBubble.Live || convention == LoadBubble.Archive)
                return basic;

            return "t" + _guid.Replace("-", "") + basic;
        }

        public override string ToString()
        {
            return base.ToString() + "(GUID:" + _guid + ")";
        }
    }
}
