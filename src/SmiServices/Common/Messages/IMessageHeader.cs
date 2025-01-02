using NLog;
using System;
using System.Collections.Generic;

namespace SmiServices.Common.Messages;

public interface IMessageHeader
{
    Guid MessageGuid { get; init; }

    int ProducerProcessID { get; init; }

    string ProducerExecutableName { get; init; }

    long OriginalPublishTimestamp { get; init; }

    /// <summary>
    /// The full message chain from origin to here
    /// </summary>
    Guid[] Parents { get; }

    void Populate(IDictionary<string, object> props);
    void Log(ILogger logger, LogLevel level, string message, Exception? ex = null);

    bool IsDescendantOf(IMessageHeader other);
}
