using Rdmp.Core.Curation.Data.EntityNaming;
using System;

namespace Microservices.DicomRelationalMapper.Execution.Namers
{
    public class MyFixedStagingDatabaseNamer : FixedStagingDatabaseNamer
    {
        //all injectable constructors must match
        public MyFixedStagingDatabaseNamer(string databaseName, Guid someGuid) : base(databaseName)
        {
        }
    }
}
