
using RabbitMQ.Client.Events;
using System;

namespace Smi.Common.Events
{
    public class FatalErrorEventArgs : EventArgs
    {
        public string Message { get; init; }
        public Exception? Exception { get; init; }


        public FatalErrorEventArgs(string msg, Exception? exception)
        {
            Message = msg;
            Exception = exception;
        }

        public FatalErrorEventArgs(BasicReturnEventArgs ra)
        {
            Message = $"BasicReturnEventArgs: {ra.ReplyCode} - {ra.ReplyText}. (Exchange: {ra.Exchange}, RoutingKey: {ra.RoutingKey})";
        }

        public override string ToString()
        {
            return $"{base.ToString()}, Message={Message}, Exception={Exception}, ";
        }
    }
}
