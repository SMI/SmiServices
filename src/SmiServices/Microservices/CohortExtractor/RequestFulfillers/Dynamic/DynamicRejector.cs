using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;

namespace SmiServices.Microservices.CohortExtractor.RequestFulfillers.Dynamic
{
    public class DynamicRejector : IRejector
    {
        private readonly Script<string> _script;

#pragma warning disable CA2211 // Non-constant fields should not be visible
        public static string DefaultDynamicRulesPath = "./DynamicRules.txt";
#pragma warning restore CA2211

        public DynamicRejector()
            : this(null) { }

        public DynamicRejector(string? dynamicRulesPath, IFileSystem? fileSystem = null)
        {
            dynamicRulesPath ??= DefaultDynamicRulesPath;
            fileSystem ??= new FileSystem();

            if (!fileSystem.File.Exists(dynamicRulesPath))
                throw new System.IO.FileNotFoundException($"Could not find rules file '{dynamicRulesPath}'");

            var dynamicRules = fileSystem.File.ReadAllText(dynamicRulesPath);

            if (string.IsNullOrWhiteSpace(dynamicRules))
                throw new ArgumentOutOfRangeException(nameof(dynamicRulesPath), "Rules file is empty");

            _script = CSharpScript.Create<string>(
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

#pragma warning disable IDE1006 // Naming Styles
            public IDataRecord? row { get; set; }
#pragma warning restore IDE1006 // Naming Styles
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
