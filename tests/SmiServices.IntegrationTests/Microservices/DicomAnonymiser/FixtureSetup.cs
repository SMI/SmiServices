using NUnit.Framework;
using SmiServices.UnitTests.TestCommon;
using System.IO;

namespace SmiServices.IntegrationTests.Microservices.DicomAnonymiser;

[SetUpFixture]
internal class FixtureSetup
{
    private const string TEST_CTP_JAR_VERSION = "0.7.0";

    public static string CtpJarPath;
    public static string CtpAllowlistPath;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        CtpJarPath = Path.Join(TestDirectoryHelpers.SlnDirectoryInfo().FullName, $"data/ctp/ctp-anon-cli-{TEST_CTP_JAR_VERSION}.jar");
        Assert.That(File.Exists(CtpJarPath), Is.True, $"Expected {CtpJarPath} to exist");

        CtpAllowlistPath = Path.Join(TestDirectoryHelpers.SlnDirectoryInfo().FullName, $"data/ctp/ctp-allowlist.script");
        Assert.That(File.Exists(CtpAllowlistPath), Is.True, $"Expected {CtpAllowlistPath} to exist");
    }
}
