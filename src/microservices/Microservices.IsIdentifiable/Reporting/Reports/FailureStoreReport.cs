using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using CsvHelper;
using Microservices.IsIdentifiable.Failures;
using Microservices.IsIdentifiable.Options;

namespace Microservices.IsIdentifiable.Reporting.Reports
{
    public class FailureStoreReport : FailureReport
    {
        private readonly object _odtLock = new object();
        private readonly DataTable _dtAllFailures;

        private readonly int _maxSize;

        private readonly string[] _headerRow = { "Resource", "ResourcePrimaryKey", "ProblemField", "ProblemValue", "PartWords", "PartClassifications", "PartOffsets" };

        private const string Separator = "###";

        /// <summary>
        /// Creates a new report aimed at the given resource (e.g. "MR_ImageTable")
        /// </summary>
        /// <param name="targetName"></param>
        /// <param name="maxSize">Max size of the internal store before writing out to file</param>
        public FailureStoreReport(string targetName, int maxSize)
            : base(targetName)
        {
            _dtAllFailures = new DataTable();

            foreach (string s in _headerRow)
                _dtAllFailures.Columns.Add(s);

            if (maxSize < 0)
                throw new ArgumentException("maxSize must be positive");

            _maxSize = maxSize;
        }

        public override void AddDestinations(IsIdentifiableAbstractOptions opts)
        {
            base.AddDestinations(opts);
            Destinations.ForEach(d => d.WriteHeader((from dc in _dtAllFailures.Columns.Cast<DataColumn>() select dc.ColumnName).ToArray()));
        }
        
        public override void Add(Failure failure)
        {
            lock (_odtLock)
            {
                _dtAllFailures.Rows.Add(
                    failure.Resource,
                    failure.ResourcePrimaryKey,
                    failure.ProblemField,
                    failure.ProblemValue,
                    string.Join(Separator, failure.Parts.Select(p => p.Word)),
                    string.Join(Separator, failure.Parts.Select(p => p.Classification)),
                    string.Join(Separator, failure.Parts.Select(p => p.Offset)));

                if (_dtAllFailures.Rows.Count < _maxSize)
                    return;

                CloseReportBase();
                _dtAllFailures.Clear();
            }
        }

        protected override void CloseReportBase()
        {
            Destinations.ForEach(d => d.WriteItems(_dtAllFailures));
        }

        public IEnumerable<Failure> Deserialize(FileInfo oldFile)
        {
            int lineNumber = 0;

            using (var r = new CsvReader(new StreamReader(File.OpenRead(oldFile.FullName))))
            {
                if(r.Read())
                    r.ReadHeader();
                else
                    yield break;
                lineNumber ++;
                // "Resource", "ResourcePrimaryKey", "ProblemField", "ProblemValue", "PartWords", "PartClassifications", "PartOffsets" 

                while (r.Read())
                {
                    lineNumber++;
                    var parts = new List<FailurePart>();

                    try
                    {

                        var words = r["PartWords"].Split(Separator);
                        var classes = r["PartClassifications"].Split(Separator);
                        var offsets = r["PartOffsets"].Split(Separator);

                        for(int i = 0 ; i < words.Length; i++)
                            parts.Add(new FailurePart(words[i],
                                (FailureClassification) Enum.Parse(typeof(FailureClassification), classes[i], true),
                                int.Parse(offsets[i])));
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Error Deserializing line " + lineNumber, e);
                    }

                    yield return new Failure( parts)
                    {
                        Resource = r["Resource"],
                        ResourcePrimaryKey = r["ResourcePrimaryKey"],
                        ProblemField = r["ProblemField"],
                        ProblemValue = r["ProblemValue"],
                    };

                }
            }
        }
    }
}
