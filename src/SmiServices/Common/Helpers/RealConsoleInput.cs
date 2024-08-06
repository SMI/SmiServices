using System;

namespace SmiServices.Common.Helpers
{
    /// <summary>
    /// Returns the next line from the console
    /// </summary>
    public class RealConsoleInput : IConsoleInput
    {
        public string? GetNextLine() => Console.ReadLine()?.Trim();
    }
}
