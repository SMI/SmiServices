using CommandLine;
using Microservices.CohortExtractor.Execution.RequestFulfillers.Dynamic;
using Newtonsoft.Json;
using NLog;
using Smi.Common.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;

namespace SmiServices.Applications.DynamicRulesTester;

public static class Program
{
    private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();
    private static IFileSystem _fileSystem = null!;

    public static int Main(IEnumerable<string> args, IFileSystem? fileSystem = null)
    {
        _fileSystem = fileSystem ?? new FileSystem();

        try
        {
            return SmiCliInit.ParseAndRun<DynamicRulesTesterCliOptions>(args, typeof(Program), OnParse);
        }
        catch (Exception e)
        {
            _logger.Error(e, "Unhandled exception during execution");
            return 2;
        }
    }

    private static int OnParse(GlobalOptions _, DynamicRulesTesterCliOptions cliOptions)
    {
        var dynamicRejector = new DynamicRejector(cliOptions.DynamicRulesFile, _fileSystem);

        using var stream = _fileSystem.File.OpenRead(cliOptions.TestRowFile);
        using var reader = new System.IO.StreamReader(stream);
        var jsonString = reader.ReadToEnd();

        if (string.IsNullOrWhiteSpace(jsonString))
        {
            _logger.Error("Test record cannot be empty");
            return 1;
        }

        _logger.Debug($"Loaded test row JSON:\n{jsonString}");

        var rowItems = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString)
            ?? throw new Exception($"Deserialized JSON was null");
        var jsonFileRecord = new JsonFileRecord(rowItems);

        if (dynamicRejector.Reject(jsonFileRecord, out string? reason))
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

    internal class DynamicRulesTesterCliOptions : CliOptions
    {
        [Option(
            'd',
            "dynamic-rules-file",
            Required = true,
            HelpText = "The file to load dynamic rules from"
        )]
        public string DynamicRulesFile { get; set; } = null!;

        [Option(
            'r',
            "test-row-file",
            Required = true,
            HelpText = "The JSON file containing test data for the rules"
        )]
        public string TestRowFile { get; set; } = null!;
    }

    [ExcludeFromCodeCoverage]
    internal class JsonFileRecord : IDataRecord
    {
        private readonly IDictionary<string, string> _items;

        public JsonFileRecord(Dictionary<string, string> items)
        {
            _items = items;
        }

        public object this[string name] => _items[name];

        public object this[int i] => throw new NotImplementedException();
        public int FieldCount => throw new NotImplementedException();
        public bool GetBoolean(int i) => throw new NotImplementedException();
        public byte GetByte(int i) => throw new NotImplementedException();
        public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length) => throw new NotImplementedException();
        public char GetChar(int i) => throw new NotImplementedException();
        public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length) => throw new NotImplementedException();
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
