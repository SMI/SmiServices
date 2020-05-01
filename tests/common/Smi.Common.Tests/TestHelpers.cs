using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace Smi.Common.Tests
{
    public class TestHelpers
    {
        // Assert two strings match apart from line endings
        public static void AreEqualIgnoringCaseAndLineEndings(string a,string b)
        {
            StringAssert.AreEqualIgnoringCase(a.Replace("\r\n","\n"), b.Replace("\r\n", "\n"));
        }
    }
}
