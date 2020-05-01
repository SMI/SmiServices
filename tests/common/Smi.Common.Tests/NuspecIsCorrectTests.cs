
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

        // Applications
        [TestCase("../../../../../../../src/applications/Applications.DicomDirectoryProcessor/Applications.DicomDirectoryProcessor.csproj", null, null)]
        [TestCase("../../../../../../../src/applications/IsIdentifiableReviewer/IsIdentifiableReviewer.csproj", null, null)]

        // Common
        [TestCase("../../../../../../../src/common/Smi.Common/Smi.Common.csproj", null, null)]
        [TestCase("../../../../../../../src/common/Smi.Common.MongoDb/Smi.Common.MongoDb.csproj", null, null)]

        // Microservices
        [TestCase("../../../../../../../src/microservices/Microservices.CohortExtractor/Microservices.CohortExtractor.csproj", null, null)]
        [TestCase("../../../../../../../src/microservices/Microservices.CohortPackager/Microservices.CohortPackager.csproj", null, null)]
        [TestCase("../../../../../../../src/microservices/Microservices.DeadLetterReprocessor/Microservices.DeadLetterReprocessor.csproj", null, null)]
        [TestCase("../../../../../../../src/microservices/Microservices.DicomRelationalMapper/Microservices.DicomRelationalMapper.csproj", null, null)]
        [TestCase("../../../../../../../src/microservices/Microservices.DicomReprocessor/Microservices.DicomReprocessor.csproj", null, null)]
        [TestCase("../../../../../../../src/microservices/Microservices.DicomTagReader/Microservices.DicomTagReader.csproj", null, null)]
        [TestCase("../../../../../../../src/microservices/Microservices.IdentifierMapper/Microservices.IdentifierMapper.csproj", null, null)]
        [TestCase("../../../../../../../src/microservices/Microservices.MongoDbPopulator/Microservices.MongoDbPopulator.csproj", null, null)]
        [TestCase("../../../../../../../src/microservices/Microservices.IsIdentifiable/Microservices.IsIdentifiable.csproj", null, null)]

        public void TestDependencyCorrect(string csproj, string nuspec, string packagesMarkdown)
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
                Assert.Fail("Could not find file {0}", csproj);
            if (nuspec != null && !File.Exists(nuspec))
                Assert.Fail("Could not find file {0}", nuspec);

            if (packagesMarkdown != null && !File.Exists(packagesMarkdown))
                Assert.Fail("Could not find file {0}", packagesMarkdown);

            //<PackageReference Include="NUnit3TestAdapter" Version="3.13.0" />
            Regex rPackageRef = new Regex(@"<PackageReference\s+Include=""(.*)""\s+Version=""([^""]*)""",
                RegexOptions.IgnoreCase);

            //<dependency id="CsvHelper" version="12.1.2" />
            Regex rDependencyRef =
                new Regex(@"<dependency\s+id=""(.*)""\s+version=""([^""]*)""", RegexOptions.IgnoreCase);

            //For each dependency listed in the csproj
            foreach (Match p in rPackageRef.Matches(File.ReadAllText(csproj)))
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

                        if (packageDependency.Equals(package))
                        {
                            Assert.AreEqual(version, versionDependency,
                                "Package {0} is version {1} in {2} but version {3} in {4}", package, version, csproj,
                                versionDependency, nuspec);
                            found = true;
                        }
                    }

                    if (!found)
                        Assert.Fail(
                            "Package {0} in {1} is not listed as a dependency of {2}. Recommended line is:\r\n{3}",
                            package, csproj, nuspec,
                            BuildRecommendedDependencyLine(package, version));
                }


                //And make sure it appears in the packages.md file
                if (packagesMarkdown != null)
                {
                    found = false;
                    foreach (string line in File.ReadAllLines(packagesMarkdown))
                    {
                        if (Regex.IsMatch(line, @"[\s[]" + Regex.Escape(package) + @"[\s\]]", RegexOptions.IgnoreCase))
                        {
                            int count = new Regex(Regex.Escape(version)).Matches(line).Count;

                            Assert.GreaterOrEqual(count, 2,
                                "Markdown file {0} did not contain 2 instances of the version {1} for package {2} in {3}",
                                packagesMarkdown, version, package, csproj);
                            found = true;
                        }
                    }

                    if (!found)
                        Assert.Fail("Package {0} in {1} is not documented in {2}. Recommended line is:\r\n{3}", package,
                            csproj, packagesMarkdown,
                            BuildRecommendedMarkdownLine(package, version));
                }
            }
        }

        [Test]
        public void VersionIsCorrectTest()
        {
            var readmeMd = File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../../../README.md"));
            var m = Regex.Match(readmeMd, "Version: `(.*)`");
            Assert.IsTrue(m.Success, "README.md in root did not list the version in the expected format");

            var readmeMdVersion = m.Groups[1].Value;

            var sharedAssemblyInfo = File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../../../src/SharedAssemblyInfo.cs"));
            var version = Regex.Match(sharedAssemblyInfo, @"AssemblyInformationalVersion\(""(.*)""\)").Groups[1].Value;

            Assert.AreEqual(version, readmeMdVersion, "README.md in root did not match version in SharedAssemblyInfo.cs");
        }

        private static object BuildRecommendedDependencyLine(string package, string version) => $"<dependency id=\"{package}\" version=\"{version}\" />";

        private static object BuildRecommendedMarkdownLine(string package, string version) => $"| {package} | [GitHub]() | [{version}](https://www.nuget.org/packages/{package}/{version}) | | | |";
    }
}
