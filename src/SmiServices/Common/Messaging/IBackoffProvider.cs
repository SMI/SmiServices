using System;

namespace SmiServices.Common.Messaging;

public interface IBackoffProvider
{
    TimeSpan GetNextBackoff();

    void Reset();
}
