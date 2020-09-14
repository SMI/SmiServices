
using RabbitMQ.Client.Events;
using System;

namespace Smi.Common.Events
{
    public class FatalErrorEventArgs : EventArgs
    {
        public string Message { get; }
        public Exception Exception { get; }


        public FatalErrorEventArgs(string msg, Exception exception)
        {
            Message = msg;
            Exception = exception;
        }

        public FatalErrorEventArgs(BasicReturnEventArgs ra)
        {
            Message = string.Format("BasicReturnEventArgs: {0} - {1}. (Exchange: {2}, RoutingKey: {3})",
                ra.ReplyCode, ra.ReplyText, ra.Exchange, ra.RoutingKey);
        }

        public override string ToString()
        {
            return ""
                + $"{base.ToString()}, "
                + $"Message={Message}, "
                + $"Exception={Exception}, "
                + "";
        }
    }
}
