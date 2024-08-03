using NUnit.Framework;

namespace Smi.Common.Tests
{
    public class TestHelpers
    {
        // Assert two strings match apart from line endings
        public static void AreEqualIgnoringCaseAndLineEndings(string a, string b)
        {
            Assert.That(b.Replace("\r\n", "\n"), Is.EqualTo(a.Replace("\r\n", "\n")).IgnoreCase);
        }

        // Assert two strings match apart from line endings, case sensitive
        public static void AreEqualIgnoringLineEndings(string a, string b)
        {
            Assert.That(b.Replace("\r\n", "\n"), Is.EqualTo(a.Replace("\r\n", "\n")).IgnoreCase);
        }

        public static void Contains(string needle, string haystack)
        {
            Assert.That(haystack.Replace("\r\n", "\n"), Does.Contain(needle.Replace("\r\n", "\n")));
        }
        public static void DoesNotContain(string needle, string haystack)
        {
            Assert.That(haystack.Replace("\r\n", "\n"), Does.Not.Contain(needle.Replace("\r\n", "\n")));
        }
    }
}
