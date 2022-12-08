﻿
using System.Threading;
using FAnsi.Discovery;
using Microservices.IdentifierMapper.Execution.Swappers;
using Smi.Common;
using Smi.Common.Options;


namespace Microservices.IdentifierMapper.Tests
{
    internal class SwapForFixedValueTester : SwapIdentifiers
    {
        private readonly string _swapForString;


        public SwapForFixedValueTester(string swapForString)
        {
            _swapForString = swapForString;
        }


        public override void Setup(IMappingTableOptions mappingTableOptions) { }

        public override string GetSubstitutionFor(string toSwap, out string reason)
        {
            reason = null;
            Success++;
            CacheHit++;
            
            using(new TimeTracker(DatabaseStopwatch))
                Thread.Sleep(500);

            return _swapForString;
        }

        public override void ClearCache() { }

        public override DiscoveredTable GetGuidTableIfAny(IMappingTableOptions options)
        {
            return null;
        }
    }
}
