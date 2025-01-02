using System;

namespace SmiServices.UnitTests.Common;

public class TestException : Exception
{
    public override string StackTrace { get; } = "StackTrace";

    public TestException(string message) : base(message, new Exception("InnerException")) { }
}
