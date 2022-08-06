using Equ;
using System;

namespace Smi.Common.Messages
{
    public class FatalErrorMessage : MemberwiseEquatable<FatalErrorMessage>, IMessage
    {
        public string Message { get; set; }

        public Exception Exception { get; set; }


        public FatalErrorMessage(string message, Exception exception)
        {
            Message = message;
            Exception = exception;
        }
    }
}