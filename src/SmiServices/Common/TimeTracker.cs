using System;
using System.Diagnostics;

namespace SmiServices.Common
{
    /// <summary>
    /// Runs a <see cref="Stopwatch"/> for the duration of the using statement (this class is <see cref="IDisposable"/>)
    /// </summary>
    public class TimeTracker : IDisposable
    {
        private readonly Stopwatch _sw;

        /// <summary>
        /// Starts the <paramref name="sw"/> and runs it until disposal (use this in a using statement)
        /// </summary>
        /// <param name="sw"></param>
        public TimeTracker(Stopwatch sw)
        {
            _sw = sw;
            _sw.Start();
        }

        /// <summary>
        /// Stops the <see cref="Stopwatch"/>
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            _sw.Stop();
        }
    }
}
