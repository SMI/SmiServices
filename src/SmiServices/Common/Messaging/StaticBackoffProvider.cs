using System;

namespace SmiServices.Common.Messaging
{
    public class StaticBackoffProvider : IBackoffProvider
    {
        private readonly TimeSpan _initialBackoff;

        public StaticBackoffProvider(TimeSpan? initialBackoff = null)
        {
            _initialBackoff = initialBackoff ?? new TimeSpan(hours: 0, minutes: 1, seconds: 0);
        }

        public TimeSpan GetNextBackoff() => _initialBackoff;

        public void Reset() { }
    }
}
