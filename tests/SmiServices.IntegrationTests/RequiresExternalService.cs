using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using System;
using System.Runtime.InteropServices;

namespace SmiServices.IntegrationTests
{
    public abstract class RequiresExternalService : CategoryAttribute, IApplyToContext
    {
        private static readonly bool _failIfUnavailable;
        private static readonly bool _ignoreIfWinCiSkip;
        private static bool _cached = false;
        private static string? _cache = null;

        static RequiresExternalService()
        {
            var ci = Environment.GetEnvironmentVariable("CI");
            if (!string.IsNullOrWhiteSpace(ci) && (ci == "1" || ci.Equals("TRUE", StringComparison.OrdinalIgnoreCase)))
                _failIfUnavailable = true;

            if (
                Environment.GetEnvironmentVariable("CI_SKIP_WIN_SERVICES") == "1"
                && RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)
            )
                _ignoreIfWinCiSkip = true;
        }

        public void ApplyToContext(TestExecutionContext context)
        {
            if (_ignoreIfWinCiSkip)
                Assert.Ignore("CI_SKIP_WIN_SERVICES");

            if (!_cached)
            {
                _cached = true;
                _cache = ApplyToContextImpl();
            }

            if (_cache is null)
            {
                if (this is RequiresRabbit r)
                    r.CheckExchange();
                return;
            }

            if (_failIfUnavailable)
                Assert.Fail(_cache);
            else
                Assert.Ignore(_cache);
        }

        protected abstract string? ApplyToContextImpl();
    }
}
