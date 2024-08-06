using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RabbitMQ.Client.Events;
using SmiServices.Common.Messages;
using System.Collections.Generic;
using System.Text;

namespace SmiServices.Common.MessageSerialization
{
    /// <summary>
    /// Helper class to (de)serialize objects from RabbitMQ messages.
    /// </summary>
    public static class JsonConvert
    {
        private static List<string> _errors = new();

        private static readonly JsonSerializerSettings _serializerSettings = new()
        {
            Error = delegate (object? sender, ErrorEventArgs args)
            {
                _errors.Add(args.ErrorContext.Error.Message);
                args.ErrorContext.Handled = true;
            },
            MissingMemberHandling = MissingMemberHandling.Error
        };


        /// <summary>
        /// Deserialize a message from a string.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="IMessage"/> to deserialize into.</typeparam>
        /// <param name="message">The message to deserialize.</param>
        /// <returns></returns>
        public static T DeserializeObject<T>(string message) where T : IMessage
        {
            _errors = new List<string>();

            var messageObj = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(message, _serializerSettings)
                ?? throw new JsonSerializationException("Deserialized message object is null, message was empty.");

            if (_errors.Count == 0)
                return messageObj;

            var e = new JsonSerializationException("Couldn't deserialize message to " + typeof(T).FullName + ". See exception data.");

            for (var i = 0; i < _errors.Count; i++)
                e.Data.Add(i, _errors[i]);

            throw e;
        }

        /// <summary>
        /// Deserialize a message straight from the <see cref="BasicDeliverEventArgs"/>. Encoding defaults to UTF8 if not set.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="IMessage"/> to deserialize into.</typeparam>
        /// <param name="deliverArgs">The message and all associated information.</param>
        /// <returns></returns>
        public static T DeserializeObject<T>(BasicDeliverEventArgs deliverArgs) where T : IMessage
        {
            Encoding enc = Encoding.UTF8;

            if (deliverArgs.BasicProperties != null && deliverArgs.BasicProperties.ContentEncoding != null)
                enc = Encoding.GetEncoding(deliverArgs.BasicProperties.ContentEncoding);

            //TODO This might crash if for some reason we have invalid Unicode points
            return DeserializeObject<T>(enc.GetString(deliverArgs.Body.Span));
        }

        public static T DeserializeObject<T>(byte[] body) where T : IMessage
        {
            Encoding enc = Encoding.UTF8;
            return DeserializeObject<T>(enc.GetString(body));
        }
    }
}
