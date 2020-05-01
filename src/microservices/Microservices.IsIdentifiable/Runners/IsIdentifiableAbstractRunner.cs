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
        /// However this looks more like a time than a date and I would argue that times are not PII?
        /// It's also not restrictive enough so matches too many non-PII numerics.
        /// </summary>
        readonly Regex _date = new Regex(
            @"\b\d+([:\-/\\]\d+)+\s?((AM)|(PM)|(GMT))?\b", RegexOptions.IgnoreCase);

        // The following regex were adapted from:
        // https://www.oreilly.com/library/view/regular-expressions-cookbook/9781449327453/ch04s04.html
        // Separators are space slash dash

        /// <summary>
        /// Matches year last, i.e d/m/y or m/d/y
        /// </summary>
        readonly Regex _dateYearLast = new Regex(
		    @"\b(?:(1[0-2]|0?[1-9])[ ]?[/-][ ]?(3[01]|[12][0-9]|0?[1-9])|(3[01]|[12][0-9]|0?[1-9])[ ]?[/-][ ]?(1[0-2]|0?[1-9]))[ ]?[/-][ ]?(?:[0-9]{2})?[0-9]{2}(\b|T)" // year last
        );
        /// <summary>
        /// Matches year first, i.e y/m/d or y/d/m
        /// </summary>
        readonly Regex _dateYearFirst = new Regex(
	    	@"\b(?:[0-9]{2})?[0-9]{2}[ ]?[/-][ ]?(?:(1[0-2]|0?[1-9])[ ]?[/-][ ]?(3[01]|[12][0-9]|0?[1-9])|(3[01]|[12][0-9]|0?[1-9])[ ]?[/-][ ]?(1[0-2]|0?[1-9]))(\b|T)" // year first
        );
        /// <summary>
        /// Matches year missing, i.e d/m or m/d
        /// </summary>
        readonly Regex _dateYearMissing = new Regex(
    		@"\b(?:(1[0-2]|0?[1-9])[ ]?[/-][ ]?(3[01]|[12][0-9]|0?[1-9])|(3[01]|[12][0-9]|0?[1-9])[ ]?[/-][ ]?(1[0-2]|0?[1-9]))(\b|T)" // year missing
        );


        /// <summary>
        /// List of columns/tags which should not be processed.  This is automatically handled by the <see cref="Validate"/> method.
        /// <para>This is a case insensitive hash collection based on <see cref="IsIdentifiableAbstractOptions.SkipColumns"/></para>
        /// </summary>
        private readonly HashSet<string> _skipColumns = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);

        private HashSet<string> _whiteList;

        /// <summary>
        /// Custom rules you want to apply e.g. always ignore column X if value is Y
        /// </summary>
        public List<ICustomRule> CustomRules { get; set; } = new List<ICustomRule>();

        /// <summary>
        /// Custom whitelist rules you want to apply e.g. always ignore a failure if column is X AND value is Y
        /// </summary>
        public List<ICustomRule> CustomWhiteListRules { get; set; } = new List<ICustomRule>();

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

            if (!string.IsNullOrWhiteSpace(opts.RulesFile))
            {
                var fi = new FileInfo(_opts.RulesFile);
                if (fi.Exists)
                    LoadRules(File.ReadAllText(fi.FullName));
                else
                    throw new Exception("Error reading "+_opts.RulesFile);
            }

            if (!string.IsNullOrWhiteSpace(opts.RulesDirectory))
            {
                DirectoryInfo di = new DirectoryInfo(opts.RulesDirectory);
                foreach (var fi in di.GetFiles("*.yaml"))
                {
                    _logger.Info($"Loading rules from {fi.Name}");
                    LoadRules(File.ReadAllText(fi.FullName));
                }
            }

            IWhitelistSource source = null;

            try
            {
                source = GetWhitelistSource();
            }
            catch (Exception e)
            {
                throw new Exception("Error getting Whitelist Source", e);
            }
            
            if (source != null)
            {
                _logger.Info("Fetching Whitelist...");
                try
                {
                    _whiteList = new HashSet<string>(source.GetWhitelist(),StringComparer.CurrentCultureIgnoreCase);
                }
                catch (Exception e)
                {
                    throw new Exception($"Error fetching values for IWhitelistSource {source.GetType().Name}", e);
                }

                _logger.Info($"Whitelist built with {_whiteList.Count} exact strings");
            }
        }

        /// <summary>
        /// Deserializes the given <paramref name="yaml"/> into a collection of <see cref="IsIdentifiableRule"/>
        /// which are added to <see cref="CustomRules"/>
        /// </summary>
        /// <param name="yaml"></param>
        public void LoadRules(string yaml)
        {
            _logger.Info("Loading Rules Yaml");
            _logger.Debug("Loading Rules Yaml:" +Environment.NewLine+yaml);
            var deserializer = new Deserializer();
            var ruleSet = deserializer.Deserialize<RuleSet>(yaml);

            if(ruleSet.BasicRules != null)
                CustomRules.AddRange(ruleSet.BasicRules);

            if(ruleSet.SocketRules != null)
                CustomRules.AddRange(ruleSet.SocketRules);

            if(ruleSet.WhiteListRules != null)
                CustomWhiteListRules.AddRange(ruleSet.WhiteListRules);
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

            //if there is a whitelist and it says to ignore the (full string) value
            if (_whiteList != null && _whiteList.Contains(fieldValue.Trim()))
                yield break;
                    
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
                        {
                            bool whitelisted = false;
                            foreach (WhiteListRule whiterule in CustomWhiteListRules)
                            {
                                switch (whiterule.ApplyWhiteListRule(fieldName, fieldValue, p))
                                {
                                    case RuleAction.Ignore: whitelisted = true; break;
                                    case RuleAction.None:
                                    case RuleAction.Report: break;
                                    default: throw new ArgumentOutOfRangeException();
                                }
                                if (whitelisted)
                                    break;
                            }
                            if (!whitelisted)
                                yield return p;
                        }
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
                foreach (Match m in _dateYearFirst.Matches(fieldValue))
                    yield return new FailurePart(m.Value.TrimEnd(), FailureClassification.Date, m.Index);

                foreach (Match m in _dateYearLast.Matches(fieldValue))
                    yield return new FailurePart(m.Value.TrimEnd(), FailureClassification.Date, m.Index);

                // XXX this may cause a duplicate failure if one above yields
                foreach (Match m in _dateYearMissing.Matches(fieldValue))
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
                _logger.Info($"Loaded a whitelist from {tbl.GetFullyQualifiedName()}");
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
