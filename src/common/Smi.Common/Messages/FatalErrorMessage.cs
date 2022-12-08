
using Equ;
using Newtonsoft.Json;
using System;

namespace Smi.Common.Messages
{
    public class FatalErrorMessage : MemberwiseEquatable<FatalErrorMessage>, IMessage
    {
        [JsonProperty(Required = Required.Always)]
        public string Message { get; set; }

        [JsonProperty(Required = Required.DisallowNull)]
        public Exception Exception { get; set; }


        public FatalErrorMessage(string message, Exception exception)
        {
            Message = message;
            Exception = exception;
        }
    }
}
