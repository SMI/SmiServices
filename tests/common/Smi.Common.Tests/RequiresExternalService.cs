using System;
using NUnit.Framework;

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
        }
    }
}
