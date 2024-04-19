using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;

namespace Microservices.CohortExtractor.Execution.RequestFulfillers.Dynamic
{
    public class DynamicRejector : IRejector
    {
        private readonly Script<string> _script;
        private const string DefaultDynamicRulesPath = "./DynamicRules.txt";

        public DynamicRejector()
        {
            // TODO(rkm 2024-04-19) Fix/rework MicroserviceObjectFactory and CohortExtractor to allow overriding these
            var dynamicRulesPath = DefaultDynamicRulesPath;
            var fileSystem = new FileSystem();

            _script = LoadScript(dynamicRulesPath, fileSystem);
        }

        public DynamicRejector(string? dynamicRulesPath, IFileSystem? fileSystem = null)
        {
            dynamicRulesPath ??= DefaultDynamicRulesPath;
            fileSystem ??= new FileSystem();

            _script = LoadScript(dynamicRulesPath, fileSystem);
        }

        private static Script<string> LoadScript(string dynamicRulesPath, IFileSystem fileSystem)
        {
            if (!fileSystem.File.Exists(dynamicRulesPath))
                throw new System.IO.FileNotFoundException($"Could not find rules file '{dynamicRulesPath}'");

            var dynamicRules = fileSystem.File.ReadAllText(dynamicRulesPath);

            if (string.IsNullOrWhiteSpace(dynamicRules))
                throw new ArgumentOutOfRangeException("Rules file is empty");

            return CSharpScript.Create<string>(
                dynamicRules,
                ScriptOptions.Default.WithReferences(typeof(Convert).Assembly).WithWarningLevel(0),
                typeof(Payload)
            );
        }

        public class Payload
        {
            public Payload(IDataRecord dbDataReader)
            {
                row = dbDataReader;
            }

            // ReSharper disable once InconsistentNaming (use this name because it should look like a local in the script).
            public IDataRecord? row { get; set; }
        }

        /// <inheritdoc/>
        public bool Reject(IDataRecord row, [NotNullWhen(true)] out string? reason)
        {
            var result = _script.RunAsync(globals: new Payload(row)).Result;

            if (result.Exception != null)
                throw result.Exception;

            reason = result.ReturnValue;

            return !string.IsNullOrWhiteSpace(reason);
        }
    }
}
