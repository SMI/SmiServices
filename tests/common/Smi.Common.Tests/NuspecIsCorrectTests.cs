
using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;


namespace Smi.Common.Tests
{
    /// <summary>
    /// Tests to confirm that the dependencies in csproj files (NuGet packages) match those in the .nuspec files and that packages.md 
    /// lists the correct versions (in documentation)
    /// </summary>
    [TestFixture]
    public class NuspecIsCorrectTests
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestLogger.Setup();
        }

        private const string RelativePackagesRoot = "../../../../../../../PACKAGES.md";
        private static readonly string[] _analyzers = { "SecurityCodeScan" };

        // TODO(rkm 2021-12-17) There are projects missing here. We should update the test so it gathers packages from all csprojs (and Directory.Build.props) automatically

        // Applications
        [TestCase("../../../../../../../src/applications/Applications.DicomDirectoryProcessor/Applications.DicomDirectoryProcessor.csproj", null, null)]

        // Common
        [TestCase("../../../../../../../src/common/Smi.Common/Smi.Common.csproj", null, null)]
        [TestCase("../../../../../../../src/common/Smi.Common.MongoDb/Smi.Common.MongoDb.csproj", null, null)]

        // Microservices
        [TestCase("../../../../../../../src/microservices/Microservices.CohortExtractor/Microservices.CohortExtractor.csproj", null, null)]
        [TestCase("../../../../../../../src/microservices/Microservices.CohortPackager/Microservices.CohortPackager.csproj", null, null)]
        [TestCase("../../../../../../../src/microservices/Microservices.DicomRelationalMapper/Microservices.DicomRelationalMapper.csproj", null, null)]
        [TestCase("../../../../../../../src/microservices/Microservices.DicomReprocessor/Microservices.DicomReprocessor.csproj", null, null)]
        [TestCase("../../../../../../../src/microservices/Microservices.DicomTagReader/Microservices.DicomTagReader.csproj", null, null)]
        [TestCase("../../../../../../../src/microservices/Microservices.IdentifierMapper/Microservices.IdentifierMapper.csproj", null, null)]
        [TestCase("../../../../../../../src/microservices/Microservices.MongoDbPopulator/Microservices.MongoDbPopulator.csproj", null, null)]
        [TestCase("../../../../../../../src/microservices/Microservices.IsIdentifiable/Microservices.IsIdentifiable.csproj", null, null)]

        public void TestDependencyCorrect(string csproj, string? nuspec, string? packagesMarkdown)
        {
            if (csproj != null && !Path.IsPathRooted(csproj))
                csproj = Path.Combine(TestContext.CurrentContext.TestDirectory, csproj);

            if (nuspec != null && !Path.IsPathRooted(nuspec))
                nuspec = Path.Combine(TestContext.CurrentContext.TestDirectory, nuspec);

            if (packagesMarkdown != null && !Path.IsPathRooted(packagesMarkdown))
                packagesMarkdown = Path.Combine(TestContext.CurrentContext.TestDirectory, packagesMarkdown);
            else
                packagesMarkdown = Path.Combine(TestContext.CurrentContext.TestDirectory, RelativePackagesRoot);

            if (!File.Exists(csproj))
                Assert.Fail($"Could not find file {csproj}");
            if (nuspec != null && !File.Exists(nuspec))
                Assert.Fail($"Could not find file {nuspec}");

            if (packagesMarkdown != null && !File.Exists(packagesMarkdown))
                Assert.Fail($"Could not find file {packagesMarkdown}");

            //<PackageReference Include="NUnit3TestAdapter" Version="3.13.0" />
            Regex rPackageRef = new(@"<PackageReference\s+Include=""(.*)""\s+Version=""([^""]*)""",
                RegexOptions.IgnoreCase);

            //<dependency id="CsvHelper" version="12.1.2" />
            Regex rDependencyRef =
                new(@"<dependency\s+id=""(.*)""\s+version=""([^""]*)""", RegexOptions.IgnoreCase);

            //For each dependency listed in the csproj
            foreach (Match p in rPackageRef.Matches(File.ReadAllText(csproj!)))
            {
                string package = p.Groups[1].Value;
                string version = p.Groups[2].Value;

                // NOTE(rkm 2020-02-14) Fix for specifiers which contain lower or upper bounds
                if (version.Contains("[") || version.Contains("("))
                    version = version.Substring(1, 5);

                bool found = false;

                //analyzers do not have to be listed as a dependency in nuspec (but we should document them in packages.md)
                if (!_analyzers.Contains(package) && nuspec != null)
                {
                    //make sure it appears in the nuspec
                    foreach (Match d in rDependencyRef.Matches(File.ReadAllText(nuspec)))
                    {
                        string packageDependency = d.Groups[1].Value;
                        string versionDependency = d.Groups[2].Value;

                        if (!packageDependency.Equals(package)) continue;
                        Assert.That(versionDependency,Is.EqualTo(version),$"Package {package} is version {version} in {csproj} but version {versionDependency} in {nuspec}");
                        found = true;
                    }

                    if (!found)
                        Assert.Fail(
$"Package {package} in {csproj} is not listed as a dependency of {nuspec}. Recommended line is:\r\n{BuildRecommendedDependencyLine(package, version)}");
                }


                //And make sure it appears in the packages.md file
                if (packagesMarkdown == null) continue;
                found = false;
                foreach (string line in File.ReadAllLines(packagesMarkdown))
                {
                    if (Regex.IsMatch(line, @"[\s[]" + Regex.Escape(package) + @"[\s\]]", RegexOptions.IgnoreCase))
                    {
                        found = true;
                    }
                }

                if (!found)
                    Assert.Fail($"Package {package} in {csproj} is not documented in {packagesMarkdown}. Recommended line is:\r\n{BuildRecommendedMarkdownLine(package, version)}");
            }
        }

        private static object BuildRecommendedDependencyLine(string package, string version) => $"<dependency id=\"{package}\" version=\"{version}\" />";

        private static object BuildRecommendedMarkdownLine(string package, string version) => $"| {package} | [GitHub]() | [{version}](https://www.nuget.org/packages/{package}/{version}) | | | |";
    }
}
