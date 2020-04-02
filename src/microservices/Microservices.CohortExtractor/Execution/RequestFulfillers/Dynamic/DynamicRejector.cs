﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Microservices.CohortExtractor.Execution.RequestFulfillers.Dynamic
{
    public class DynamicRejector : IRejector
    {
        private string _dynamicRules;
        private Script<string> _script;
        public const string DynamicRulesPath = "./DynamicRules.txt";
        public DynamicRejector()
        {
            if(!File.Exists(DynamicRulesPath))
                throw new FileNotFoundException($"Could not find rules file '{DynamicRulesPath}'");
            
            _dynamicRules = File.ReadAllText(DynamicRulesPath);

            try
            {
                _script = CSharpScript.Create<string>(_dynamicRules,
                    ScriptOptions.Default.WithReferences(typeof(Convert).Assembly),typeof(Payload));
                _script.Compile();
            }
            catch (CompilationErrorException e)
            {
                throw new Exception($"Failed to compile {DynamicRulesPath} " + string.Join(Environment.NewLine, e.Diagnostics),e);
            }
        }

        public class Payload
        {
            public Payload(DbDataReader dbDataReader)
            {
                row = dbDataReader;
            }

            // ReSharper disable once InconsistentNaming (use this name because it should look like a local in the script).
            public DbDataReader row { get; set; }
        }

        public bool Reject(DbDataReader row, out string reason)
        {
            var result = _script.RunAsync(globals: new Payload(row)).Result;

            if (result.Exception != null)
                throw result.Exception;

            reason = result.ReturnValue;
            
            return !string.IsNullOrWhiteSpace(reason);
        }
    }
}
