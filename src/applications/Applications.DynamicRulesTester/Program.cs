using CommandLine;
using JetBrains.Annotations;
using Microservices.CohortExtractor.Execution.RequestFulfillers.Dynamic;
using Newtonsoft.Json;
using NLog;
using Smi.Common.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Applications.DynamicRulesTester;

public static class Program
{
    private static ILogger _logger = LogManager.GetCurrentClassLogger();

    public static int Main(IEnumerable<string> args)
    {
        return SmiCliInit.ParseAndRun<DynamicRulesTesterCliOptions>(args, typeof(Program), OnParse);
    }

    private static int OnParse(GlobalOptions _, DynamicRulesTesterCliOptions cliOptions)
    {
        var dynamicRejector = new DynamicRejector(cliOptions.DynamicRulesFile);
        var jsonRecord = new JsonFileRecord(cliOptions.TestRowFile);

        if (dynamicRejector.Reject(jsonRecord, out string reason))
        {
            _logger.Warn($"Rejection reason was:'{reason}'");
            return 1;
        }
        else
        {
            _logger.Info("Record was accepted");
            return 0;
        }
    }

    [UsedImplicitly]
    internal class DynamicRulesTesterCliOptions : CliOptions
    {
        [Option(
            'd',
            "dynamic-rules-file",
            Required = true,
            HelpText = "The file to load dynamic rules from"
        )]
        public string DynamicRulesFile { get; set; }

        [Option(
            'r',
            "test-row-file",
            Required = true,
            HelpText = "The JSON file containing test data for the rules"
        )]
        public string TestRowFile { get; set; }
    }

    internal class JsonFileRecord : IDataRecord
    {
        private readonly IDictionary<string, string> _items;

        public JsonFileRecord(string fileName)
        {
            using var reader = new StreamReader(fileName);
            var jsonString = reader.ReadToEnd();
            _logger.Debug($"Loaded test row JSON:\n{jsonString}");

            _items = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);
        }

        public object this[string name] => _items[name];

        public object this[int i] => throw new NotImplementedException();
        public int FieldCount => throw new NotImplementedException();
        public bool GetBoolean(int i) => throw new NotImplementedException();
        public byte GetByte(int i) => throw new NotImplementedException();
        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length) => throw new NotImplementedException();
        public char GetChar(int i) => throw new NotImplementedException();
        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length) => throw new NotImplementedException();
        public IDataReader GetData(int i) => throw new NotImplementedException();
        public string GetDataTypeName(int i) => throw new NotImplementedException();
        public DateTime GetDateTime(int i) => throw new NotImplementedException();
        public decimal GetDecimal(int i) => throw new NotImplementedException();
        public double GetDouble(int i) => throw new NotImplementedException();
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
        public Type GetFieldType(int i) => throw new NotImplementedException();
        public float GetFloat(int i) => throw new NotImplementedException();
        public Guid GetGuid(int i) => throw new NotImplementedException();
        public short GetInt16(int i) => throw new NotImplementedException();
        public int GetInt32(int i) => throw new NotImplementedException();
        public long GetInt64(int i) => throw new NotImplementedException();
        public string GetName(int i) => throw new NotImplementedException();
        public int GetOrdinal(string name) => throw new NotImplementedException();
        public string GetString(int i) => throw new NotImplementedException();
        public object GetValue(int i) => throw new NotImplementedException();
        public int GetValues(object[] values) => throw new NotImplementedException();
        public bool IsDBNull(int i) => throw new NotImplementedException();
    }
}
