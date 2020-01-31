using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FAnsi;
using FAnsi.Discovery;
using Microservices.IsIdentifiable.Failures;
using Microservices.IsIdentifiable.Options;
using Microservices.IsIdentifiable.Reporting.Reports;
using Microservices.IsIdentifiable.Rules;
using Microservices.IsIdentifiable.Whitelists;
using NLog;
using YamlDotNet.Serialization;

namespace Microservices.IsIdentifiable.Runners
{
    public abstract class IsIdentifiableAbstractRunner : IDisposable
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        private readonly IsIdentifiableAbstractOptions _opts;

        public readonly List<IFailureReport> Reports = new List<IFailureReport>();

        // DDMMYY + 4 digits 
        // \b bounded i.e. not more than 10 digits
        readonly Regex _chiRegex = new Regex(@"\b[0-3][0-9][0-1][0-9][0-9]{6}\b");
        readonly Regex _postcodeRegex = new Regex(@"\b((GIR 0AA)|((([A-Z-[QVX]][0-9][0-9]?)|(([A-Z-[QVX]][A-Z-[IJZ]][0-9][0-9]?)|(([A-Z-[QVX]][0-9][A-HJKSTUW])|([A-Z-[QVX]][A-Z-[IJZ]][0-9][ABEHMNPRVWXY]))))\s?[0-9][A-Z-[CIKMOV]]{2}))\b", RegexOptions.IgnoreCase);

        /// <summary>
        /// Matches a 'symbol' (digit followed by an optional th, rd or separator) then a month name (e.g. Jan or January)
        /// </summary>
        readonly Regex _symbolThenMonth = new Regex(@"\d+((th)|(rd)|(st)|[\-/\\])?\s?((Jan(uary)?)|(Feb(ruary)?)|(Mar(ch)?)|(Apr(il)?)|(May)|(June?)|(July?)|(Aug(ust)?)|(Sep(tember)?)|(Oct(ober)?)|(Nov(ember)?)|(Dec(ember)?))", RegexOptions.IgnoreCase);

        /// <summary>
        /// Matches a month name (e.g. Jan or January) followed by a 'symbol' (digit followed by an optional th, rd or separator) then a
        /// </summary>
        readonly Regex _monthThenSymbol = new Regex(@"((Jan(uary)?)|(Feb(ruary)?)|(Mar(ch)?)|(Apr(il)?)|(May)|(June?)|(July?)|(Aug(ust)?)|(Sep(tember)?)|(Oct(ober)?)|(Nov(ember)?)|(Dec(ember)?))[\s\-/\\]?\d+((th)|(rd)|(st))?", RegexOptions.IgnoreCase);

        /// <summary>
        /// Matches digits followed by a separator (: - \ etc) followed by more digits with optional AM / PM / GMT at the end
        /// </summary>
        readonly Regex _date = new Regex(
            @"\b\d+([:\-/\\]\d+)+\s?((AM)|(PM)|(GMT))?\b", RegexOptions.IgnoreCase);

        /// <summary>
        /// List of columns/tags which should not be processed.  This is automatically handled by the <see cref="Validate"/> method.
        /// <para>This is a case insensitive hash collection based on <see cref="IsIdentifiableAbstractOptions.SkipColumns"/></para>
        /// </summary>
        private readonly HashSet<string> _skipColumns = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);

        /// <summary>
        /// Custom rules you want to apply e.g. always ignore column X if value is Y
        /// </summary>
        public List<ICustomRule> CustomRules { get; set; } = new List<ICustomRule>();

        protected IsIdentifiableAbstractRunner(IsIdentifiableAbstractOptions opts)
        {
            _opts = opts;
            _opts.ValidateOptions();
            
            string targetName = _opts.GetTargetName();

            if (opts.ColumnReport)
                Reports.Add(new ColumnFailureReport(targetName));

            if (opts.ValuesReport)
                Reports.Add(new FailingValuesReport(targetName));

            if (opts.StoreReport)
                Reports.Add(new FailureStoreReport(targetName, _opts.MaxCacheSize));
            
            if (!Reports.Any())
                throw new Exception("No reports have been specified, use the relevant command line flag e.g. --ColumnReport");

            Reports.ForEach(r => r.AddDestinations(_opts));

            if (!string.IsNullOrWhiteSpace(_opts.SkipColumns))
                foreach (string c in _opts.SkipColumns.Split(','))
                    _skipColumns.Add(c);

            var fi = new FileInfo("Rules.yaml");
            
            if (fi.Exists)
                LoadRules(File.ReadAllText(fi.FullName));
            else
                _logger.Info("No Rules Yaml file found (thats ok)");
        }

        /// <summary>
        /// Deserializes the given <paramref name="yaml"/> into a collection of <see cref="IsIdentifiableRule"/>
        /// which are added to <see cref="CustomRules"/>
        /// </summary>
        /// <param name="yaml"></param>
        public void LoadRules(string yaml)
        {
            _logger.Info("Loading Rules Yaml:" + Environment.NewLine + yaml);
            var deserializer = new Deserializer();
            var ruleSet = deserializer.Deserialize<RuleSet>(yaml);

            if(ruleSet.BasicRules != null)
                CustomRules.AddRange(ruleSet.BasicRules);

            if(ruleSet.SocketRules != null)
                CustomRules.AddRange(ruleSet.SocketRules);
        }

        // ReSharper disable once UnusedMemberInSuper.Global
        public abstract int Run();

        /// <summary>
        /// Returns each subsection of <paramref name="fieldValue"/> which violates validation rules (e.g. the CHI found).
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="fieldValue"></param>
        /// <returns></returns>
        protected IEnumerable<FailurePart> Validate(string fieldName, string fieldValue)
        {
            if (_skipColumns.Contains(fieldName))
                yield break;

            if (string.IsNullOrWhiteSpace(fieldValue))
                yield break;

            // Carets (^) are synonymous with space in some dicom tags
            fieldValue = fieldValue.Replace('^', ' ');
            
            //for each custom rule
            foreach (ICustomRule rule in CustomRules)
            {
                switch (rule.Apply(fieldName, fieldValue, out IEnumerable<FailurePart> parts))
                {
                    case RuleAction.None:
                        break;
                    //if rule is to skip the cell (i.e. don't run other classifiers)
                    case RuleAction.Ignore:
                        yield break;
                    
                    //if the rule is to report it then report as a failure but also run other classifiers
                    case RuleAction.Report:
                        foreach (var p in parts)
                            yield return p;

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            //does the string contain chis?
            foreach (Match m in _chiRegex.Matches(fieldValue))
                yield return new FailurePart(m.Value, FailureClassification.PrivateIdentifier, m.Index);

            if (!_opts.IgnorePostcodes)
                foreach (Match m in _postcodeRegex.Matches(fieldValue))
                    yield return new FailurePart(m.Value, FailureClassification.Postcode, m.Index);

            if (!_opts.IgnoreDatesInText)
            {
                foreach (Match m in _date.Matches(fieldValue))
                    yield return new FailurePart(m.Value.TrimEnd(), FailureClassification.Date, m.Index);

                foreach (Match m in _symbolThenMonth.Matches(fieldValue))
                    yield return new FailurePart(m.Value.TrimEnd(), FailureClassification.Date, m.Index);

                foreach (Match m in _monthThenSymbol.Matches(fieldValue))
                    yield return new FailurePart(m.Value.TrimEnd(), FailureClassification.Date, m.Index);

            }
        }

        /// <summary>
        /// Records the provided failure to all selected reports
        /// </summary>
        /// <param name="f"></param>
        protected void AddToReports(Reporting.Failure f)
        {
            Reports.ForEach(r => r.Add(f));
        }

        /// <summary>
        /// Tells all selected reports that the <paramref name="numberOfRowsDone"/> have been processed (this is a += operation 
        /// not substitution i.e. call it with 10 then 10 again then 10 leads to 30 rows done.
        /// </summary>
        /// <param name="numberOfRowsDone"></param>
        protected void DoneRows(int numberOfRowsDone)
        {
            Reports.ForEach(r => r.DoneRows(numberOfRowsDone));
        }

        /// <summary>
        /// Call once you have done all validation, this method will write the report results to the final destination 
        /// e.g. CSV etc
        /// </summary>
        protected void CloseReports()
        {
            Reports.ForEach(r => r.CloseReport());
        }

        private IWhitelistSource GetWhitelistSource()
        {
            IWhitelistSource source = null;

            if (!string.IsNullOrWhiteSpace(_opts.WhitelistCsv))
            {
                // If there's a file whitelist
                source = new CsvWhitelist(_opts.WhitelistCsv);
                _logger.Info($"Loaded a whitelist from {Path.GetFullPath(_opts.WhitelistCsv)}");
            }
            else if (!string.IsNullOrWhiteSpace(_opts.WhitelistConnectionString))
            {
                // If there's a database whitelist
                DiscoveredTable tbl = GetServer(_opts.WhitelistConnectionString, _opts.WhitelistDatabaseType, _opts.WhitelistTableName);
                DiscoveredColumn col = tbl.DiscoverColumn(_opts.WhitelistColumn);
                source = new DiscoveredColumnWhitelist(col);
                _logger.Info($"Loaded a whitelist from {_opts.WhitelistConnectionString} {_opts.WhitelistTableName}");
            }

            return source;
        }

        /// <summary>
        /// Connects to the specified database and returns a managed object for interacting with it.
        /// 
        /// <para>This method will check that the table exists on the server</para>
        /// </summary>
        /// <param name="databaseConnectionString">Connection string (which must include database element)</param>
        /// <param name="databaseType">The DBMS provider of the database referenced by <paramref name="databaseConnectionString"/></param>
        /// <param name="tableName">Unqualified table name e.g. "mytable"</param>
        /// <returns></returns>
        protected DiscoveredTable GetServer(string databaseConnectionString, DatabaseType databaseType, string tableName)
        {
            DiscoveredDatabase db = GetServer(databaseConnectionString, databaseType);
            DiscoveredTable tbl = db.ExpectTable(tableName);

            if (!tbl.Exists())
                throw new Exception("Table did not exist");

            _logger.Log(LogLevel.Info, "Found Table '" + tbl.GetRuntimeName() + "'");

            return tbl;
        }

        /// <summary>
        /// Connects to the specified database and returns a managed object for interacting with it.
        /// </summary>
        /// <param name="databaseConnectionString">Connection string (which must include database element)</param>
        /// <param name="databaseType">The DBMS provider of the database referenced by <paramref name="databaseConnectionString"/></param>
        /// <returns></returns>
        private static DiscoveredDatabase GetServer(string databaseConnectionString, DatabaseType databaseType)
        {
            var server = new DiscoveredServer(databaseConnectionString, databaseType);

            DiscoveredDatabase db = server.GetCurrentDatabase();

            if (db == null)
                throw new Exception("No current database");

            return db;
        }

        public virtual void Dispose()
        {
            foreach (var d in CustomRules.OfType<IDisposable>()) 
                d.Dispose();
        }
    }
}
