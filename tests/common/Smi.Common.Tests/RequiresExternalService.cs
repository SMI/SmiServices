using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using System;
using System.Runtime.InteropServices;

namespace Smi.Common.Tests
{
    public class RequiresExternalService : CategoryAttribute, IApplyToContext
    {
        protected readonly bool FailIfUnavailable;
        private readonly bool IgnoreIfWinCiSkip;

        public RequiresExternalService()
        {
            string? ci = Environment.GetEnvironmentVariable("CI");
            if (!string.IsNullOrWhiteSpace(ci) && (ci == "1" || ci.ToUpper() == "TRUE"))
                FailIfUnavailable = true;

            if (
                Environment.GetEnvironmentVariable("CI_SKIP_WIN_SERVICES") == "1"
                && RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)
            )
                IgnoreIfWinCiSkip = true;
        }

        public void ApplyToContext(TestExecutionContext context)
        {
            if (IgnoreIfWinCiSkip)
                Assert.Ignore("CI_SKIP_WIN_SERVICES");

            ApplyToContextImpl(context);
        }

        protected virtual void ApplyToContextImpl(TestExecutionContext context) { }
    }
}
