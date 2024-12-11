using NLog;
using Rdmp.Core.Curation.Data;
using SmiServices.Common.Messages.Extraction;
using SmiServices.Common.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SmiServices.Microservices.CohortExtractor.RequestFulfillers
{
    public class FromCataloguesExtractionRequestFulfiller : IExtractionRequestFulfiller
    {
        private readonly QueryToExecuteColumnSet[] _catalogues;
        private readonly ILogger _logger;

        public List<IRejector> Rejectors { get; set; } = [];
        public Dictionary<ModalitySpecificRejectorOptions, IRejector> ModalitySpecificRejectors { get; set; } = [];

        private readonly Regex _defaultModalityRoutingRegex = new("^([A-Z]+)_.*$", RegexOptions.Compiled);
        private readonly Regex _modalityRoutingRegex;

        /// <summary>
        /// </summary>
        /// <param name="cataloguesToUseForImageLookup"></param>
        /// <param name="modalityRoutingRegex">
        /// Controls how modalities are matched to Catalogues. Must contain a single capture group which
        /// returns a modality code (e.g. CT) when applies to a Catalogue name.  E.g. ^([A-Z]+)_.*$ would result
        /// in Modalities being routed based on the start of the table name e.g. CT => CT_MyTable and MR=> MR_MyTable
        /// </param>
        /// <exception cref="Exception"></exception>
        public FromCataloguesExtractionRequestFulfiller(ICatalogue[] cataloguesToUseForImageLookup, Regex? modalityRoutingRegex = null)
        {
            _logger = LogManager.GetCurrentClassLogger();

            _logger.Debug("Preparing to filter " + cataloguesToUseForImageLookup.Length + " Catalogues to look for compatible ones");

            _catalogues = cataloguesToUseForImageLookup.OrderBy(c => c.ID).Select(QueryToExecuteColumnSet.Create).Where(s => s != null).ToArray()!;

            _logger.Debug("Found " + _catalogues.Length + " Catalogues matching filter criteria");

            if (_catalogues.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(cataloguesToUseForImageLookup), "There are no compatible Catalogues in the repository (See QueryToExecuteColumnSet for required columns)");

            _modalityRoutingRegex = modalityRoutingRegex ?? _defaultModalityRoutingRegex;
            if (_modalityRoutingRegex.GetGroupNumbers().Length != 2)
                throw new ArgumentOutOfRangeException(nameof(modalityRoutingRegex), $"Must have exactly one non-default capture group");
        }

        public IEnumerable<ExtractImageCollection> GetAllMatchingFiles(ExtractionRequestMessage message)
        {
            var queries = new List<QueryToExecute>();
            var rejectors = GetRejectorsFor(message);

            foreach (var c in _catalogues.Where(x => x.Contains(message.KeyTag)))
            {
                var match = _modalityRoutingRegex.Match(c.Catalogue.Name);
                if (!match.Success)
                    continue;

                // NOTE: Match will always have two gropus as we check the regex in the constructor
                if (match.Groups[1].Value != message.Modality)
                    continue;

                var query = new QueryToExecute(c, message.KeyTag, rejectors);
                queries.Add(query);
            }

            _logger.Debug($"Found {queries.Count} Catalogues which support extracting based on '{message.KeyTag}'");

            if (queries.Count == 0)
                throw new Exception($"Couldn't find any compatible Catalogues to run extraction queries against for query {message}");

            foreach (string valueToLookup in message.ExtractionIdentifiers)
            {
                var results = new ExtractImageCollection(valueToLookup);

                foreach (QueryToExecute query in queries)
                {
                    foreach (QueryToExecuteResult result in query.Execute(valueToLookup))
                    {
                        var seriesTagValue = result.SeriesTagValue
                            ?? throw new Exception(nameof(result.SeriesTagValue));

                        if (!results.ContainsKey(seriesTagValue))
                            results.Add(seriesTagValue, []);

                        results[seriesTagValue].Add(result);
                    }
                }

                yield return results;
            }
        }

        private IEnumerable<IRejector> GetRejectorsFor(ExtractionRequestMessage message)
        {
            if (message.IsNoFilterExtraction)
                return [];

            var applicableRejectors =
                ModalitySpecificRejectors
                .Where(
                    // Do the modalities covered by this rejector apply to the images returned by the query
                    k => k.Key.GetModalities().Any(m => string.Equals(m, message.Modality, StringComparison.CurrentCultureIgnoreCase))
                    )
                .ToArray();

            // if modality specific rejectors override regular rejectors
            if (applicableRejectors.Any(r => r.Key.Overrides))
            {
                // they had better all override or none of them!
                if (!applicableRejectors.All(r => r.Key.Overrides))
                {
                    throw new Exception($"You cannot mix Overriding and non Overriding ModalitySpecificRejectors.  Bad Modality was '{message.Modality}'");
                }

                // yes we have custom rejection rules for this modality
                return applicableRejectors.Select(r => r.Value);
            }

            // The modality specific rejectors run in addition to the basic Rejectors so serve both
            return applicableRejectors.Select(r => r.Value).Union(Rejectors);
        }
    }
}
