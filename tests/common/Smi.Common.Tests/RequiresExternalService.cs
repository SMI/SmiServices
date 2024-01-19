using NUnit.Framework;
using System;
using System.Runtime.InteropServices;

namespace Smi.Common.Tests
{
    public class RequiresExternalService : CategoryAttribute
    {
        protected readonly bool FailIfUnavailable;

        public RequiresExternalService()
        {
            string? ci = Environment.GetEnvironmentVariable("CI");
            if (!string.IsNullOrWhiteSpace(ci) && (ci == "1" || ci.ToUpper() == "TRUE"))
                FailIfUnavailable = true;

            if (
                Environment.GetEnvironmentVariable("CI_SKIP_WIN_SERVICES") == "1" &&
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.Ignore("Requires external service");
            }
        }
    }
}
