using NLog;
using System;
using System.Collections.Generic;

namespace Smi.Common.Messages
{
    public interface IMessageHeader
    {
        Guid MessageGuid { get; }

        int ProducerProcessID { get; }

        string ProducerExecutableName { get; }

        long OriginalPublishTimestamp { get; }

        /// <summary>
        /// The full message chain from origin to here
        /// </summary>
        Guid[] Parents { get; }

        void Populate(IDictionary<string, object> props);
        void Log(ILogger logger, LogLevel level, string message, Exception ex = null);

        bool IsDescendantOf(IMessageHeader other);
    }
}
