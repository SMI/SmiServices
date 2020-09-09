using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using YamlDotNet.Serialization;

namespace Smi.Common.Options
{
    public class GlobalOptionsFactory
    {
        public List<IOptionsDecorator> Decorators {get;set;} = new List<IOptionsDecorator>();

        public GlobalOptionsFactory()
        {
            Decorators.Add(new EnvironmentVariableDecorator());
        }
        public GlobalOptions Load(string environment = "default", string currentDirectory = null)
        {
            IDeserializer deserializer = new DeserializerBuilder()
                                    .WithObjectFactory(GetGlobalOption)
                                    .IgnoreUnmatchedProperties()
                                    .Build();

            currentDirectory = currentDirectory ?? Environment.CurrentDirectory;

            // Make sure environment ends with yaml 
            if (!(environment.EndsWith(".yaml") || environment.EndsWith(".yml")))
                environment += ".yaml";

            // If the yaml file doesn't exist and the path is relative, try looking in currentDirectory instead
            if (!File.Exists(environment) && !Path.IsPathRooted(environment))
                environment = Path.Combine(currentDirectory, environment);

            string text = File.ReadAllText(environment);

            var globals = deserializer.Deserialize<GlobalOptions>(new StringReader(text));
            globals.CurrentDirectory = currentDirectory;
            globals.MicroserviceOptions = new MicroserviceOptions();

            return Decorate(globals);
        }

        /// <summary>
        /// Applies all <see cref="Decorators"/> to <paramref name="globals"/>
        /// </summary>
        /// <param name="globals"></param>
        /// <returns></returns>
        private GlobalOptions Decorate(GlobalOptions globals)
        {
            foreach(var d in Decorators)
                globals = d.Decorate(globals);

            return globals;
        }

        public GlobalOptions Load(CliOptions cliOptions)
        {
            //load but do not decorate
            GlobalOptions globalOptions = Load(cliOptions.YamlFile,null);
            globalOptions.MicroserviceOptions = new MicroserviceOptions(cliOptions);

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
