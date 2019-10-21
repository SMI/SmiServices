
using CommandLine;
using CommandLine.Text;
using Dicom;
using Microservices.Common.Options;
using ReusableLibraryCode.Annotations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DicomRepopulator.Options
{
    /// <summary>
    /// Options class for Dicom Repopulator
    /// </summary>
    //TODO This shouldn't really be a subclass of CliOptions
    public class DicomRepopulatorOptions : CliOptions
    {
        private const string DefaultOutputDirName = "defaultOut";

        #region CLI Options

        [Option('i', "input-csv", Required = true, HelpText = "CSV file containing the data")]
        [UsedImplicitly]
        public string CsvFilePath { get; set; }

        [Option('d', "input-directory", Required = true, HelpText = "Directory containing the dicom files to repopulate")]
        [UsedImplicitly]
        public string DirectoryToProcessPath { get; set; }

        [Option('o', "output-directory", Required = false, HelpText = "Output directory. \"" + DefaultOutputDirName + "\" is used if not specified")]
        [UsedImplicitly]
        public string OutputDirectoryPath { get; set; }

        [Option('k', "key", Required = true, HelpText = "CSVHeaderName:DicomTagName for key")]
        [UsedImplicitly]
        public string Key { get; set; }

        [Option('m', "mappings", Required = true, HelpText = "CSVHeaderName:DicomTagName mappings")]
        [UsedImplicitly]
        public IEnumerable<string> Mappings { get; set; }

        [Option('t', "threads", Default = 4, Required = false, HelpText = "Max. number of threads to use when processing")]
        [UsedImplicitly]
        public int NumThreads { get; set; }

        #endregion

        private FileInfo _csvFileInfo;

        /// <summary>
        /// Specifies the input CSV file.
        /// </summary>
        public FileInfo CsvFileInfo
        {
            get
            {
                if (_csvFileInfo != null)
                    return _csvFileInfo;

                _csvFileInfo = new FileInfo(CsvFilePath);

                if (!_csvFileInfo.Exists)
                    throw new Exception("Could not find the csv input file (" + CsvFilePath + ")");

                // Set to null to avoid using unvalidated inputs by mistake
                CsvFilePath = null;

                return _csvFileInfo;
            }
        }

        private DirectoryInfo _directoryToProcessInfo;

        /// <summary>
        /// Specifies the input directory to process.
        /// </summary>
        public DirectoryInfo DirectoryToProcessInfo
        {
            get
            {
                if (_directoryToProcessInfo != null)
                    return _directoryToProcessInfo;

                _directoryToProcessInfo = new DirectoryInfo(DirectoryToProcessPath);

                if (!_directoryToProcessInfo.Exists)
                    throw new Exception("Could not find the directory to process (" + DirectoryToProcessPath + ")");

                // Set to null to avoid using unvalidated inputs by mistake
                DirectoryToProcessPath = null;

                return _directoryToProcessInfo;
            }
        }

        private DirectoryInfo _outputDirectoryInfo;

        /// <summary>
        /// Specifies the output directory.
        /// </summary>
        public DirectoryInfo OutputDirectoryInfo
        {
            get
            {
                if (_outputDirectoryInfo != null)
                    return _outputDirectoryInfo;

                if (string.IsNullOrWhiteSpace(OutputDirectoryPath))
                {
                    if (DirectoryToProcessInfo.Parent == null)
                        throw new Exception("Parent directory of input was null");

                    _outputDirectoryInfo = Directory.CreateDirectory(Path.Combine(DirectoryToProcessInfo.Parent.FullName, DefaultOutputDirName));
                }
                else
                {
                    _outputDirectoryInfo = new DirectoryInfo(OutputDirectoryPath);

                    if (!_outputDirectoryInfo.Exists)
                    {
                        try
                        {
                            Directory.CreateDirectory(_outputDirectoryInfo.FullName);
                        }
                        catch (Exception e)
                        {
                            throw new Exception("Could not find the output directory (" + OutputDirectoryPath + ")", e);
                        }
                    }
                }

                // Set to null to avoid using unvalidated inputs by mistake
                OutputDirectoryPath = null;

                return _outputDirectoryInfo;
            }
        }

        private DicomTag _keyDicomTag;
        /// <summary>
        /// Dicom Tag that is used as to key to match between the Dicom file and the CSV file
        /// </summary>
        public DicomTag KeyDicomTag
        {
            get
            {
                if (_keyDicomTag == null) GetKeyDicomTagAndColumnName();
                return _keyDicomTag;
            }
        }

        private string _keyColumnName;
        /// <summary>
        /// CSV column name that is used as to key to match between the Dicom file and the CSV file
        /// </summary>
        public string KeyColumnName
        {
            get
            {
                if (_keyColumnName == null) GetKeyDicomTagAndColumnName();
                return _keyColumnName;
            }
        }

        /// <summary>
        /// Get the key DicomTag and associated CSV column name.
        /// </summary>
        private void GetKeyDicomTagAndColumnName()
        {
            string[] split = Key.Split(':');
            if (split.Length != 2 || split[0].Length == 0 || split[1].Length == 0)
            {
                throw new Exception("Key not in format <CSVColumnName>:<DicomTagName>: " + Key);
            }
            string dicomTagString = split[1];

            // Check the DICOM tag is a valid DICOM tag
            DicomDictionaryEntry prasedDictEntry =
                DicomDictionary.Default.SingleOrDefault(entry => entry.Keyword == dicomTagString);

            if (prasedDictEntry == null)
            {
                throw new Exception(
                    "Error in key '" + Key + "', " + dicomTagString + " is not a valid Dicom tag.");
            }

            _keyDicomTag = prasedDictEntry.Tag;
            _keyColumnName = split[0];
        }

        // Mapping from Dicom Tags to CSV column headers to DCOM tags.
        private Dictionary<DicomTag, string> _mappingDictionary;

        /// <summary>
        /// Dictionary that maps from Dicom tags to CSV column names.
        /// </summary>
        public Dictionary<DicomTag, string> MappingDictionary
        {
            get
            {
                if (_mappingDictionary != null) return _mappingDictionary;

                _mappingDictionary = new Dictionary<DicomTag, string>();

                foreach (var mapping in Mappings)
                {
                    string[] split = mapping.Split(':');
                    if (split.Length != 2 || split[0].Length == 0 || split[1].Length == 0)
                    {
                        throw new Exception("Mapping not in format <CSVColumnName>:<DicomTagName>: " + mapping);
                    }
                    string dicomTagString = split[1];

                    // Check the DICOM tag is a valid DICOM tag
                    DicomDictionaryEntry prasedDictEntry = DicomDictionary.Default.SingleOrDefault(entry => entry.Keyword == dicomTagString);

                    if (prasedDictEntry == null)
                    {
                        throw new Exception(
                            "Error in mapping '" + mapping + "', " + dicomTagString + " is not a valid Dicom tag.");
                    }

                    _mappingDictionary.Add(prasedDictEntry.Tag, split[0]);
                }

                return _mappingDictionary;
            }
        }

        [Usage]
        [UsedImplicitly]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return
                    new Example("Normal Scenario", new DicomRepopulatorOptions { CsvFilePath = @"my.csv", DirectoryToProcessPath = @"c:\temp\", OutputDirectoryPath = @"c:\temp\out\", Key = "Id:PatientID", Mappings = new List<string> { "Date:SeriesDate", "AcqDt:AcquisitionDateTime" } });
            }
        }

        public bool Validate()
        {
            try
            {
                FileInfo unused1 = CsvFileInfo;
                DirectoryInfo unused2 = DirectoryToProcessInfo;
                DirectoryInfo unused3 = OutputDirectoryInfo;
                Dictionary<DicomTag, string> unused4 = MappingDictionary;
            }
            catch (Exception e)
            {
                Console.WriteLine("Err: Could not validate cli options: " + e.Message + "\n");
                return false;
            }

            return true;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine("CsvFile: " + CsvFileInfo.FullName);
            sb.AppendLine("DirectoryToProcess: " + DirectoryToProcessInfo.FullName);
            sb.AppendLine("OutputDirectory: " + OutputDirectoryInfo.FullName);
            sb.AppendLine("Key: " + KeyColumnName + ":" + KeyDicomTag.DictionaryEntry.Keyword);
            sb.AppendLine("Mappings: ");

            foreach (KeyValuePair<DicomTag, string> mapping in MappingDictionary)
                sb.AppendLine("\t" + mapping.Value + ":" + mapping.Key.DictionaryEntry.Keyword);

            return sb.ToString();
        }
    }
}
