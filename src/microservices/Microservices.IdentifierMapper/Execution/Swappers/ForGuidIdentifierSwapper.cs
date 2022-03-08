using System;

namespace Microservices.IdentifierMapper.Execution.Swappers
{
    public class ForGuidIdentifierSwapper : ReplacementValueIdentifierSwapper
    {
        protected override string GetReplacementValue() => Guid.NewGuid().ToString();
    }
}
