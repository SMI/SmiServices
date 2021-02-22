using Microsoft.Extensions.FileSystemGlobbing;
using NUnit.Framework;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Smi.Common.Tests
{
    public class CsprojTests
    {
        #region Fixture Methods

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestLogger.Setup();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() { }

        #endregion

        #region Test Methods

        [SetUp]
        public void SetUp() { }

        [TearDown]
        public void TearDown() { }

        #endregion

        #region Tests

        /// <summary>
        /// Checks that no PackageReferences are defined in any csproj file. Packages should only
        /// be defined in the Directory.Build.props files.
        /// </summary>
        [Test]
        public void NoPackageReferenceInCsprojs()
        {
            string path = Directory.GetCurrentDirectory();
            while (true)
            {
                if (File.Exists(Path.Join(path, "SmiServices.sln")))
                    break;
                path = Path.Combine(path, "..");
            }

            var matcher = new Matcher();
            matcher.AddInclude("**/*.csproj");
            var matches = matcher.GetResultsInFullPath(path);
            var packageReferenceRegex = new Regex(@"PackageReference", RegexOptions.IgnoreCase);
            
            foreach (string csproj in matches)
                Assert.False(
                    packageReferenceRegex.IsMatch(File.ReadAllText(csproj)),
                    $"Found a PackageReference in {csproj}"
                );
        }

        #endregion
    }
}
