using Smi.Common.Options;
using NUnit.Framework;

namespace Smi.Common.Tests.Options
{
    public class FileSystemOptionsTests
    {
        [Test]
        public void TestFileSystemOptions_AsLinuxRootDir()
        {

            var opts = new FileSystemOptions
            {
                FileSystemRoot = "/",
                ExtractRoot = "/",
            };

            Assert.Multiple(() =>
            {
                Assert.That(opts.FileSystemRoot, Is.EqualTo("/"));
                Assert.That(opts.ExtractRoot, Is.EqualTo("/"));
            });
        }
    }
}
