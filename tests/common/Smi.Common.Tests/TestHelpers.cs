using NUnit.Framework;
using System.IO;

namespace Smi.Common.Tests
{
    public class TestHelpers
    {
        // Assert two strings match apart from line endings
        public static void AreEqualIgnoringCaseAndLineEndings(string a, string b)
        {
            StringAssert.AreEqualIgnoringCase(a.Replace("\r\n", "\n"), b.Replace("\r\n", "\n"));
        }

        // Assert two strings match apart from line endings, case sensitive
        public static void AreEqualIgnoringLineEndings(string a, string b)
        {
            StringAssert.AreEqualIgnoringCase(a.Replace("\r\n", "\n"), b.Replace("\r\n", "\n"));
        }

        public static void Contains(string needle, string haystack)
        {
            StringAssert.Contains(needle.Replace("\r\n", "\n"), haystack.Replace("\r\n", "\n"));
        }

        /// <summary>
        /// Walks up the directory tree until the project root directory (containing SmiServices.sln) is found, and returns it.
        /// </summary>
        /// <returns></returns>
        public static string GetProjectRoot()
        {
            string path = Directory.GetCurrentDirectory();
            while (true)
            {
                if (File.Exists(Path.Join(path, "SmiServices.sln")))
                    break;
                path = Path.Combine(path, "..");
            }
            return path;
        }
    }
}
