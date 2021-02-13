using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;

namespace Smi.Common.Options
{
    public class GlobalOptionsFactory
    {
        private readonly List<IOptionsDecorator> _decorators = new List<IOptionsDecorator>();

        /// <summary>
        /// Create a GlobalOptionsFactory with the given set of <see cref="IOptionsDecorator"/>s. Adds a single <see cref="EnvironmentVariableDecorator"/> by default if passed a null value.
        /// </summary>
        /// <param name="decorators"></param>
        public GlobalOptionsFactory(
            [CanBeNull] ICollection<IOptionsDecorator> decorators = null
        )
        {
            if (decorators != null)
                _decorators.AddRange(decorators);
            else
                _decorators.Add(new EnvironmentVariableDecorator());
        }

        public GlobalOptions Load(string configFilePath = "default.yaml")
        {
            IDeserializer deserializer = new DeserializerBuilder()
                                    .WithObjectFactory(GetGlobalOption)
                                    .IgnoreUnmatchedProperties()
                                    .Build();

            if (!File.Exists(configFilePath))
                throw new ArgumentException($"Could not find config file '{configFilePath}'");

            string yamlContents = File.ReadAllText(configFilePath);
            var globals = deserializer.Deserialize<GlobalOptions>(new StringReader(yamlContents));

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

        public GlobalOptions Load(CliOptions cliOptions)
        {
            GlobalOptions globalOptions = Load(cliOptions.YamlFile);
            
            // The above Load call does the decoration - don't do it here.
            return globalOptions;
        }

        private object GetGlobalOption(Type arg)
        {
            return arg == typeof(GlobalOptions) ?
                new GlobalOptions() :
                Activator.CreateInstance(arg);
        }
    }
}
