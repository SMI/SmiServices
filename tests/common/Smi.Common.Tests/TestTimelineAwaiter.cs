using System;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;

namespace Smi.Common.Tests
{
    /// <summary>
    /// helper for asynchronous tests, awaits for certain conditions to be true within a given timeout (or infinite timeout if debugger is attached)
    /// </summary>
    public class TestTimelineAwaiter
    {
        public void Await(Func<bool> condition,string timeoutMessage= null,int timeout = 30000)
        {
            if (Debugger.IsAttached)
                timeout = int.MaxValue;

            while (!condition() && timeout > 0)
            {
                Thread.Sleep(100);
                timeout -= 100;
            }

            if (timeout <= 0)
                Assert.Fail(timeoutMessage ?? "Failed to reach the condition after the expected timeout");
        }
    }
}
