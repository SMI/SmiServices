
using Equ;
using Newtonsoft.Json;
using System;

namespace SmiServices.Common.Messages
{
    public class FatalErrorMessage : MemberwiseEquatable<FatalErrorMessage>, IMessage
    {
        [JsonProperty(Required = Required.Always)]
        public string Message { get; set; } = null!;

        // TODO(rkm 2023-08-04) The nullability is confusing here. We should audit and remove all DisallowNull usages
        [JsonProperty(Required = Required.DisallowNull)]
        public Exception? Exception { get; set; }

        public FatalErrorMessage(string message, Exception? exception)
        {
            Message = message;
            Exception = exception;
        }
    }
}
