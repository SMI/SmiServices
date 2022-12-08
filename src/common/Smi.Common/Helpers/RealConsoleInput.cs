using System;

namespace Smi.Common.Helpers
{
    /// <summary>
    /// Returns the next line from the console
    /// </summary>
    public class RealConsoleInput : IConsoleInput
    {
        public string GetNextLine() => Console.ReadLine()?.Trim();
    }
}
