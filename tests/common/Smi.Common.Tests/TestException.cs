using System;

namespace Smi.Common.Tests
{
    public class TestException : Exception
    {
        public override string StackTrace { get; } = "StackTrace";

        public TestException(string message) : base(message, new Exception("InnerException")) { }
    }
}
