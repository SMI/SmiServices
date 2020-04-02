using System;
using System.Collections.Generic;
using System.IO;
using CsvHelper;

namespace Microservices.IsIdentifiable.Whitelists
{
    /// <summary>
    /// A Whitelist source which returns the values in the first column of the provided Csv file.  The file must be properly escaped
    /// if it has commas in fields etc.  There must be no header record.
    /// </summary>
    public class CsvWhitelist : IWhitelistSource
    {
        private readonly CsvReader _reader;

        public CsvWhitelist(string filePath)
        {
            if(!File.Exists(filePath))
                throw new Exception("Could not find whitelist file at '" + filePath +"'");
            
            _reader = new CsvReader(new StreamReader(filePath),System.Globalization.CultureInfo.CurrentCulture);
            _reader.Configuration.HasHeaderRecord = false;
        }

        public IEnumerable<string> GetWhitelist()
        {
            while (_reader.Read())
                yield return _reader[0];
        }
    }
}