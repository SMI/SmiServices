using System;

namespace SmiServices.Common.Messaging;

public class ExponentialBackoffProvider : IBackoffProvider
{
    private readonly TimeSpan _initialBackoff;
    private TimeSpan _currentBackoff;

    public ExponentialBackoffProvider(TimeSpan? initialBackoff = null)
    {
        _initialBackoff = initialBackoff ?? new TimeSpan(hours: 0, minutes: 1, seconds: 0);
        Reset();
    }

    public TimeSpan GetNextBackoff()
    {
        var b = _currentBackoff;
        _currentBackoff *= 2;
        return b;
    }

    public void Reset()
    {
        _currentBackoff = _initialBackoff;
    }
}
