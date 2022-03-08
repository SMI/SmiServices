using Smi.Common.Helpers;

namespace Microservices.IdentifierMapper.Execution.Swappers
{
    public class ForGeneratedUIDIdentifierSwapper : ReplacementValueIdentifierSwapper
    {
        protected override string GetReplacementValue() => SmiDicomUIDGenerator.Generate();
    }
}
