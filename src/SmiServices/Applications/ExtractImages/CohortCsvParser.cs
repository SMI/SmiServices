using CsvHelper;
using CsvHelper.Configuration;
using SmiServices.Common.Messages.Extraction;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;


namespace SmiServices.Applications.ExtractImages;

public class CohortCsvParser
{
    // NOTE(rkm 2021-04-01) Just do a simple line-by-line read through the CSV
    private static readonly CsvConfiguration _csvConfiguration = new(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = false
    };

    private readonly IFileSystem _fileSystem;

    public CohortCsvParser(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public Tuple<ExtractionKey, List<string>> Parse(string csvFilePath)
    {
        using var fileStream = _fileSystem.FileStream.New(csvFilePath, FileMode.Open, FileAccess.Read);
        using var streamReader = new StreamReader(fileStream);
        using var reader = new CsvReader(streamReader, _csvConfiguration);

        reader.Read();
        var headerRecord = reader.Parser.Record ?? throw new ApplicationException(message: "CSV is empty");

        if (headerRecord.Length != 1)
            throw new ApplicationException(message: "CSV must have exactly 1 column");

        if (!Enum.TryParse<ExtractionKey>(headerRecord[0], out var extractionKey))
        {
            var keys = string.Join(separator: ',', Enum.GetNames(typeof(ExtractionKey)));
            throw new ApplicationException($"CSV header must be a valid ExtractionKey: {keys}");
        }

        var allIds = new List<string>();
        while (reader.Read())
        {
            var record = reader.Parser.Record;
            if (record.Length != 1)
                throw new ApplicationException(message: "CSV must have exactly 1 column");

            var id = record[0]?.Trim();
            if (!string.IsNullOrWhiteSpace(id))
                allIds.Add(id);
        }

        if (allIds.Count == 0)
            throw new ApplicationException(message: "No records in the cohort CSV");

        return new Tuple<ExtractionKey, List<string>>(extractionKey, allIds);
    }
}
