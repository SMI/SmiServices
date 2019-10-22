
using Newtonsoft.Json;
using System;

namespace Smi.Common.Messages
{
    public class FatalErrorMessage : IMessage
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

        #region Equality Members

        private bool Equals(FatalErrorMessage other)
        {
            return string.Equals(Message, other.Message) && Equals(Exception, other.Exception);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((FatalErrorMessage)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Message != null ? Message.GetHashCode() : 0) * 397) ^ (Exception != null ? Exception.GetHashCode() : 0);
            }
        }

        #endregion
    }
}