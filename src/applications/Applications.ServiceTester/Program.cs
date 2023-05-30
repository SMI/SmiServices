using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Smi.Common;
using Smi.Common.Events;
using Smi.Common.Messages;
using Smi.Common.MessageSerialization;
using Smi.Common.Options;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using JsonConvert = Newtonsoft.Json.JsonConvert;

namespace Applications.ServiceTester
{
    [ExcludeFromCodeCoverage]
    public static class Program
    {
        private static readonly Assembly SmiCommonAssembly = typeof(IMessage).Assembly;

        // NOTE(rkm 2023-04-11) Unused, but silences a warning message when constructing RabbitMqAdapter
        private static event HostFatalHandler OnFatal;

        public static int Main(IEnumerable<string> args)
        {
            int ret = SmiCliInit
                .ParseAndRun<ServiceTesterCliOptions>(
                    args,
                    typeof(Program),
                    OnParse
                );
            return ret;
        }

        private static int OnParse(GlobalOptions globals, ServiceTesterCliOptions parsedOptions)
        {
            Type messageType;
            object message;

            if (!string.IsNullOrWhiteSpace(parsedOptions.PrintMessageTemplate))
            {
                messageType = GetTypeFromAssembly(parsedOptions.PrintMessageTemplate);
                message = Activator.CreateInstance(messageType);
                var options = new JsonSerializerSettings()
                {
                    Converters = { new TemplatingConverter() },
                };
                Console.WriteLine(JsonConvert.SerializeObject(message, Formatting.Indented, options));
                return 0;
            }
            else if (
                string.IsNullOrWhiteSpace(parsedOptions.MessageFilePath) ||
                string.IsNullOrWhiteSpace(parsedOptions.ExchangeName))
            {
                Console.Error.WriteLine($"MessageFilePath and ExchangeName are required for sending messages");
                return 1;
            }

            var messageFile = JObject.Parse(File.ReadAllText(parsedOptions.MessageFilePath));
            messageType = GetTypeFromAssembly(messageFile["class"].ToString());
            message = messageFile.ToObject(messageType);

            var connectionFactory = globals.RabbitOptions.CreateConnectionFactory();
            var hostId = globals.HostProcessName + Environment.ProcessId;

            var adapter = new RabbitMqAdapter(connectionFactory, hostId, OnFatal);

            var producerOptions = new ProducerOptions()
            {
                ExchangeName = parsedOptions.ExchangeName,
                MaxConfirmAttempts = 1,
            };
            var producerModel = adapter.SetupProducer(producerOptions);

            Console.WriteLine($"Sending {messageType.Name}:\n{JsonConvert.SerializeObject(message)}");
            producerModel.SendMessage((IMessage)message, null, parsedOptions.RoutingKey ?? "");
            Console.WriteLine("... sent!");

            return 0;
        }

        private static Type GetTypeFromAssembly(string typeName)
        {
            var type = SmiCommonAssembly.GetTypes().FirstOrDefault(x => x.Name == typeName);
            return type ?? throw new ArgumentException($"Could not find type '{typeName}' in {SmiCommonAssembly}");
        }

        private class TemplatingConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType) => true;

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) => throw new NotImplementedException();

            /// <summary>
            /// Here be dragons
            /// </summary>
            /// <param name="writer"></param>
            /// <param name="value"></param>
            /// <param name="serializer"></param>
            /// <exception cref="NotImplementedException"></exception>
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                writer.WriteStartObject();

                writer.WritePropertyName("class");
                writer.WriteValue(value.GetType().Name);

                foreach (var property in value.GetType().GetProperties())
                {
                    writer.WritePropertyName(property.Name);
                    var propertyValue = property.GetValue(value) ?? "";

                    if (property.PropertyType == typeof(JsonCompatibleDictionary<MessageHeader, string>))
                    {
                        var t = new JsonCompatibleDictionary<MessageHeader, string>()
                        {
                            { new MessageHeader(), "<string>" },
                        };
                        writer.WriteValue(JsonConvert.SerializeObject(t));
                    }
                    else if (propertyValue is Array)
                    {
                        writer.WriteStartArray();
                        var arrayType = propertyValue.GetType().GetElementType();
                        if (arrayType.Name == "String")
                        {
                            writer.WriteValue("<string>");
                        }
                        else
                        {
                            writer.WriteValue(Activator.CreateInstance(arrayType));
                        }
                        writer.WriteEndArray();
                    }
                    else if (propertyValue is IList)
                    {
                        writer.WriteStartArray();
                        var listType = propertyValue.GetType().GenericTypeArguments[0];
                        if (listType.Name == "String")
                        {
                            writer.WriteValue("<string>");
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                        writer.WriteEndArray();
                    }
                    else if (propertyValue is IDictionary)
                    {
                        writer.WriteStartObject();
                        var vk = (propertyValue as IDictionary).Keys.GetType();
                        var keyType = vk.GenericTypeArguments[0];
                        var valueType = vk.GenericTypeArguments[1];
                        if (keyType.Name == "String")
                        {
                            writer.WritePropertyName("<string>");
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                        writer.WriteValue(Activator.CreateInstance(valueType));
                        writer.WriteEndObject();
                    }
                    else
                    {
                        writer.WriteValue(propertyValue);
                    }
                }

                writer.WriteEndObject();
            }
        }
    }
}
