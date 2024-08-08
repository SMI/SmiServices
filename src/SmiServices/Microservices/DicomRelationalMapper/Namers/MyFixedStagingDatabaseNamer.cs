using Rdmp.Core.Curation.Data.EntityNaming;
using System;

namespace SmiServices.Microservices.DicomRelationalMapper.Namers
{
    public class MyFixedStagingDatabaseNamer : FixedStagingDatabaseNamer
    {
        //all injectable constructors must match
        public MyFixedStagingDatabaseNamer(string databaseName, Guid _) : base(databaseName)
        {
        }
    }
}
