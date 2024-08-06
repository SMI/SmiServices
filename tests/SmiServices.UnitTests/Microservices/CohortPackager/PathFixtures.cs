using NUnit.Framework;
using NUnit.Framework.Interfaces;
using SmiServices.UnitTests.Common;
using System;
using System.IO;


namespace SmiServices.UnitTests.Microservices.CohortPackager
{
    // TODO(rkm 2020-12-17) Test if the old form of this is fixed in NUnit 3.13 (see https://github.com/nunit/nunit/issues/2574)
    internal class PathFixtures : IDisposable
    {
        public readonly string ExtractName;
        public readonly string TestDirAbsolute;
        public readonly string ExtractRootAbsolute;
        public readonly string ProjExtractionsDirRelative = Path.Combine("proj", "extractions");
        public readonly string ProjExtractDirRelative;
        public readonly string ProjExtractDirAbsolute;
        public readonly string ProjReportsDirAbsolute;

        public PathFixtures(string extractName)
        {
            ExtractName = extractName;

            TestDirAbsolute = TestFileSystemHelpers.GetTemporaryTestDirectory();

            ExtractRootAbsolute = Path.Combine(TestDirAbsolute, "extractRoot");

            ProjExtractDirRelative = Path.Combine(ProjExtractionsDirRelative, extractName);
            ProjExtractDirAbsolute = Path.Combine(ExtractRootAbsolute, ProjExtractDirRelative);

            ProjReportsDirAbsolute = Path.Combine(ExtractRootAbsolute, ProjExtractionsDirRelative, "reports");

            // NOTE(rkm 2020-11-19) This would normally be created by one of the other services
            Directory.CreateDirectory(ProjExtractDirAbsolute);
        }

        public void Dispose()
        {
            ResultState outcome = TestContext.CurrentContext.Result.Outcome;
            if (outcome == ResultState.Failure || outcome == ResultState.Error)
                return;

            Directory.Delete(TestDirAbsolute, recursive: true);
        }
    }
}
