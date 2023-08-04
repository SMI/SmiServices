using System.Collections.Generic;
using CommandLine;
using NUnit.Framework;
using Smi.Common.Options;
using Smi.Common.Tests;


namespace Applications.ExtractImages.Tests
{
    public class ExtractImagesCliOptionsTests
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

        [Test]
        public void ParseArguments()
        {
            Parser parser = SmiCliInit.GetDefaultParser();

            void Verify(IEnumerable<string> args, string? modalities, bool ident, bool noFilters)
            {
                parser.ParseArguments<ExtractImagesCliOptions>(args)
                    .WithParsed(options =>
                    {
                        Assert.AreEqual("1234-5678", options.ProjectId);
                        Assert.AreEqual("foo.csv", options.CohortCsvFile);
                        Assert.AreEqual(modalities, options.Modalities);
                        Assert.AreEqual(ident, options.IsIdentifiableExtraction);
                        Assert.AreEqual(noFilters, options.IsNoFiltersExtraction);
                    })
                    .WithNotParsed(errors => Assert.Fail(string.Join(',', errors)));
            }

            Verify(new[] { "-p", "1234-5678", "-c", "foo.csv" }, null, false, false);
            Verify(new[] { "-p", "1234-5678", "-c", "foo.csv", "-m", "CT", "-i", "-f" }, "CT", true, true);
        }

        #endregion
    }
}
