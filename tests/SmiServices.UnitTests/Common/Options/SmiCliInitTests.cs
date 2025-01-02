using CommandLine;
using NUnit.Framework;
using SmiServices.Common.Options;


namespace SmiServices.UnitTests.Common.Options;

public class SmiCliInitTests
{
    #region Fixture Methods

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        SmiCliInit.InitSmiLogging = false;
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() { }

    #endregion

    #region Test Methods

    [SetUp]
    public void SetUp() { }

    [TearDown]
    public void TearDown() { }

    private class FakeCliOpts : CliOptions
    {
        [Option(
            'f', "foo",
            Required = false
        )]
        public string? Foo { get; set; }
    }

    [Verb("fake")]
    private class FakeCliVerbOpts : CliOptions
    {
        [Option(
            'f', "foo",
            Required = false
        )]
        public string? Foo { get; set; }
    }

    #endregion

    #region Tests

    [Test]
    public void SmiCliInit_SingleParser_HappyPath()
    {
        static int OnParse(GlobalOptions globals, FakeCliOpts opts)
        {
            if (opts.Foo == "bar")
                return 123;
            return -1;
        }

        var args = new[]
        {
            "-y", "default.yaml",
            "-f", "bar"
        };

        int ret = SmiCliInit.ParseAndRun<FakeCliOpts>(args, nameof(SmiCliInit_SingleParser_HappyPath), OnParse);
        Assert.That(ret, Is.EqualTo(123));
    }

    [Test]
    public void SmiCliInit_SingleParser_Help_ReturnsZero()
    {
        var args = new[] { "--help" };

        int ret = SmiCliInit.ParseAndRun<FakeCliOpts>(args, nameof(SmiCliInit_SingleParser_Help_ReturnsZero), (_, __) => -1);
        Assert.That(ret, Is.EqualTo(0));
    }

    [Test]
    public void SmiCliInit_VerbParser_HappyPath()
    {
        static int OnParse(GlobalOptions globals, object parsed)
        {
            var opts = SmiCliInit.Verify<FakeCliVerbOpts>(parsed);

            if (opts.Foo == "bar")
                return 123;
            return -1;
        }

        var args = new[]
        {
            "fake",
            "-y", "default.yaml",
            "-f", "bar"
        };

        int ret = SmiCliInit.ParseAndRun(args, nameof(SmiCliInit_VerbParser_HappyPath), [typeof(FakeCliVerbOpts)], OnParse);
        Assert.That(ret, Is.EqualTo(123));
    }

    [Test]
    public void SmiCliInit_VerbParser_Help_ReturnsZero()
    {
        var args = new[]
        {
            "fake",
            "--help"
        };

        int ret = SmiCliInit.ParseAndRun(args, nameof(SmiCliInit_VerbParser_Help_ReturnsZero), [typeof(FakeCliVerbOpts)], (_, __) => -1);
        Assert.That(ret, Is.EqualTo(0));
    }

    #endregion
}
