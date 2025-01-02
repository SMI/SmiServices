using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using YamlDotNet.Serialization;

namespace SmiServices.Common.Options;

public class GlobalOptionsFactory
{
    private readonly List<IOptionsDecorator> _decorators = [];

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
    /// <param name="fileSystem"></param>
    /// <returns></returns>
    public GlobalOptions Load(string hostProcessName, string configFilePath = "default.yaml", IFileSystem? fileSystem = null)
    {
        fileSystem ??= new FileSystem();

        var deserializer = new DeserializerBuilder()
            .WithObjectFactory(GetGlobalOption)
            .IgnoreUnmatchedProperties()
            .Build();

        if (!fileSystem.File.Exists(configFilePath))
            throw new ArgumentException($"Could not find config file '{configFilePath}'");

        var yamlContents = fileSystem.File.ReadAllText(configFilePath);
        var globals = deserializer.Deserialize<GlobalOptions?>(yamlContents)
                      ?? throw new Exception("Did not deserialize a GlobalOptions object from the provided YAML file. Does it contain at least one valid key?");

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
    /// <param name="fileSystem"></param>
    /// <returns></returns>
    public GlobalOptions Load(string hostProcessName, CliOptions cliOptions, IFileSystem? fileSystem = null)
    {
        GlobalOptions globalOptions = Load(hostProcessName, cliOptions.YamlFile, fileSystem);

        // The above Load call does the decoration - don't do it here.
        return globalOptions;
    }

    private object GetGlobalOption(Type arg)
    {
        var opts = arg == typeof(GlobalOptions) ?
            new GlobalOptions() :
            Activator.CreateInstance(arg);
        return opts ?? throw new ArgumentException(null, nameof(arg));
    }
}
