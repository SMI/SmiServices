using System;
using System.Collections.Generic;
using System.Linq;
using FAnsi.Discovery;
using Microservices.IsIdentifiable.Failures;

namespace Microservices.IsIdentifiable.Reporting
{
    class DatabaseFailureFactory
    {
        private readonly DiscoveredTable _table;
        private readonly string _tableName;
        
        public DiscoveredColumn[] PrimaryKeys { get; private set; }

        public DatabaseFailureFactory(DiscoveredTable table)
        {
            _table = table;
            _tableName =  _table.GetFullyQualifiedName();
            PrimaryKeys = _table.DiscoverColumns().Where(c => c.IsPrimaryKey).ToArray();
        }

        public Failure Create(string field, string value, IEnumerable<FailurePart> parts, params string[] primaryKeyValues)
        {
            if(primaryKeyValues.Length != PrimaryKeys.Length)
                throw new Exception("Provided primaryKeyValues did not match the expected number found on the table:" + PrimaryKeys.Length);

            return new Failure(parts)
            {
                Resource   =  _tableName,
                ResourcePrimaryKey = string.Join(";",primaryKeyValues),
                ProblemValue = value,
                ProblemField =  field
            };
        }
    }
}