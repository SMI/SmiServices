using NUnit.Framework;
using SmiServices.Common;
using System.IO;

namespace SmiServices.UnitTests.Common;

class ZipHelperTests
{
    [TestCase("my.zip", true)]
    [TestCase("my.dcm", false)]
    [TestCase("my", false)]
    public void TestZipHelper(string input, bool expectedOutput)
    {
        Assert.That(ZipHelper.IsZip(input), Is.EqualTo(expectedOutput));

        var fs = new System.IO.Abstractions.FileSystem();
        var fi = new System.IO.Abstractions.FileInfoWrapper(fs, new FileInfo(input));
        Assert.That(ZipHelper.IsZip(fi), Is.EqualTo(expectedOutput));
    }
}
