using System;
using System.Data;
using System.Data.Common;
using System.IO;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Microservices.CohortExtractor.Execution.RequestFulfillers.Dynamic
{
    public class DynamicRejector : IRejector
    {
        private string _dynamicRules;
        private Script<string> _script;
        private const string DefaultDynamicRulesPath = "./DynamicRules.txt";

        public DynamicRejector(string dynamicRulesPath)
        {
            if(dynamicRulesPath == null)
                dynamicRulesPath = DefaultDynamicRulesPath;

            if(!File.Exists(dynamicRulesPath))
                throw new FileNotFoundException($"Could not find rules file '{dynamicRulesPath}'");
            
            _dynamicRules = File.ReadAllText(dynamicRulesPath);

            try
            {
                _script = CSharpScript.Create<string>(_dynamicRules,
                    ScriptOptions.Default.WithReferences(typeof(Convert).Assembly),typeof(Payload));
                _script.Compile();
            }
            catch (CompilationErrorException e)
            {
                throw new Exception($"Failed to compile {dynamicRulesPath} " + string.Join(Environment.NewLine, e.Diagnostics),e);
            }
        }

        public class Payload
        {
            public Payload(IDataRecord dbDataReader)
            {
                row = dbDataReader;
            }

            // ReSharper disable once InconsistentNaming (use this name because it should look like a local in the script).
            public IDataRecord row { get; set; }
        }

        /// <inheritdoc/>
        public bool Reject(IDataRecord row, out string reason)
        {
            var result = _script.RunAsync(globals: new Payload(row)).Result;

            if (result.Exception != null)
                throw result.Exception;

            reason = result.ReturnValue;
            
            return !string.IsNullOrWhiteSpace(reason);
        }
    }
}
