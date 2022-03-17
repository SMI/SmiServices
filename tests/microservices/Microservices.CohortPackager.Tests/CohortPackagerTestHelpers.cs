using Microservices.CohortPackager.Execution.ExtractJobStorage;
using System;

namespace Microservices.CohortPackager.Tests
{
    internal static class CohortPackagerTestHelpers
    {
        public static ExtractJobInfo GetRandomExtractJobInfo()
            => new(
                Guid.NewGuid(),
                DateTime.UtcNow,
                "123",
                "test/dir",
                "KeyTag",
                123,
                null,
                ExtractJobStatus.ReadyForChecks,
                isIdentifiableExtraction: true,
                isNoFilterExtraction: true
            );
    }
}
