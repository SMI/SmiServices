using System;

namespace SmiServices.Common.Messaging
{
    public class StaticBackoffProvider : IBackoffProvider
    {
        private readonly TimeSpan _backoff;

        public StaticBackoffProvider(TimeSpan? backoff = null)
        {
            _backoff = backoff ?? new TimeSpan(hours: 0, minutes: 1, seconds: 0);
        }

        public TimeSpan GetNextBackoff() => _backoff;

        public void Reset() { }
    }
}
