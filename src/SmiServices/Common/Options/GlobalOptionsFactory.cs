using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;

namespace SmiServices.Common.Options
{
    public class GlobalOptionsFactory
    {
        private readonly List<IOptionsDecorator> _decorators = new();

        /// <summary>
        /// Create a GlobalOptionsFactory with the given set of <see cref="IOptionsDecorator"/>s. Adds a single <see cref="EnvironmentVariableDecorator"/> by default if passed a null value.
        /// </summary>
        /// <param name="decorators"></param>
        public GlobalOptionsFactory(
            ICollection<IOptionsDecorator>? decorators = null
        )
        {
            if (decorators != null)
                _decorators.AddRange(decorators);
            else
                _decorators.Add(new EnvironmentVariableDecorator());
        }

        /// <summary>
        /// Loads and decorates a GlobalOptions object from the specified YAML config file
        /// </summary>
        /// <param name="hostProcessName"></param>
        /// <param name="configFilePath"></param>
        /// <returns></returns>
        public GlobalOptions Load(string hostProcessName, string configFilePath = "default.yaml")
        {
            IDeserializer deserializer = new DeserializerBuilder()
                                    .WithObjectFactory(GetGlobalOption)
                                    .IgnoreUnmatchedProperties()
                                    .Build();

            if (!File.Exists(configFilePath))
                throw new ArgumentException($"Could not find config file '{configFilePath}'");

            string yamlContents = File.ReadAllText(configFilePath);

            using var sr = new StringReader(yamlContents);
            var globals = deserializer.Deserialize<GlobalOptions>(sr);

            if (globals.LoggingOptions == null)
                throw new Exception($"Loaded YAML did not contain a {nameof(globals.LoggingOptions)} key. Did you provide a valid config file?");

            globals.HostProcessName = hostProcessName;

            return Decorate(globals);
        }

        /// <summary>
        /// Applies all <see cref="_decorators"/> to <paramref name="globals"/>
        /// </summary>
        /// <param name="globals"></param>
        /// <returns></returns>
        private GlobalOptions Decorate(GlobalOptions globals)
        {
            foreach (var d in _decorators)
                globals = d.Decorate(globals);

            return globals;
        }

        /// <summary>
        /// Loads and decorates a GlobalOptions object from the YAML config file specified in the CliOptions
        /// </summary>
        /// <param name="hostProcessName"></param>
        /// <param name="cliOptions"></param>
        /// <returns></returns>
        public GlobalOptions Load(string hostProcessName, CliOptions cliOptions)
        {
            GlobalOptions globalOptions = Load(hostProcessName, cliOptions.YamlFile);

            // The above Load call does the decoration - don't do it here.
            return globalOptions;
        }

        private object GetGlobalOption(Type arg)
        {
            var opts = arg == typeof(GlobalOptions) ?
                new GlobalOptions() :
                Activator.CreateInstance(arg);
            return opts ?? throw new ArgumentException(nameof(arg));
        }
    }
}
