using NUnit.Framework;
using System;
using System.IO;

namespace Smi.Common.Tests
{
    public static class TestFileSystemHelpers
    {
        public static string GetTemporaryTestDirectory()
        {
            string testName = TestContext.CurrentContext.Test.FullName.Replace('(', '_').Replace(")", "");
            return Path.Combine(Path.GetTempPath(), "smiservices-nunit", $"{testName}-{Guid.NewGuid().ToString().Split('-')[0]}");
        }
    }
}
