
using Microservices.IdentifierMapper.Execution.Swappers;
using Smi.Common.Options;


namespace Microservices.IdentifierMapper.Tests
{
    internal class SwapForFixedValueTester : ISwapIdentifiers
    {
        private readonly string _swapForString;


        public SwapForFixedValueTester(string swapForString)
        {
            _swapForString = swapForString;
        }


        public void Setup(IMappingTableOptions mappingTableOptions) { }

        public string GetSubstitutionFor(string toSwap, out string reason)
        {
            reason = null;
            return _swapForString;
        }

        public void ClearCache() { }
    }
}