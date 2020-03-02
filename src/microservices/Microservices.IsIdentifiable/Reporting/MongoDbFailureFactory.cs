using System.Collections.Generic;
using Microservices.IsIdentifiable.Failures;
using MongoDB.Bson;

namespace Microservices.IsIdentifiable.Reporting
{
    internal class MongoDbFailureFactory
    {
        public Failure Create(ObjectId documentId, string problemTag, string problemValue, IEnumerable<FailurePart> parts)
        {
            return new Failure(parts)
            {
                // No need to set this since the report will be named MongoDB-<database>.<collection>
                Resource = "",

                // Guaranteed to be unique across a collection
                ResourcePrimaryKey = documentId.ToString(),

                ProblemField = problemTag,
                ProblemValue = problemValue
            };
        }
    }
}
