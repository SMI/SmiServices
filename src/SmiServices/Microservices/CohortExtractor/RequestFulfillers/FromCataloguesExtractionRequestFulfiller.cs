using NLog;
using Rdmp.Core.Curation.Data;
using Smi.Common.Messages.Extraction;
using Smi.Common.Options;
using SmiServices.Microservices.CohortExtractor;
using SmiServices.Microservices.CohortExtractor.Audit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SmiServices.Microservices.CohortExtractor.RequestFulfillers
{
    public class FromCataloguesExtractionRequestFulfiller : IExtractionRequestFulfiller
    {
        protected readonly QueryToExecuteColumnSet[] Catalogues;
        protected readonly Logger Logger;

        public List<IRejector> Rejectors { get; set; } = new List<IRejector>();
        public Dictionary<ModalitySpecificRejectorOptions, IRejector> ModalitySpecificRejectors { get; set; }
            = new Dictionary<ModalitySpecificRejectorOptions, IRejector>();
        public Regex? ModalityRoutingRegex { get; set; }

        public FromCataloguesExtractionRequestFulfiller(ICatalogue[] cataloguesToUseForImageLookup)
        {
            Logger = LogManager.GetCurrentClassLogger();

            Logger.Debug("Preparing to filter " + cataloguesToUseForImageLookup.Length + " Catalogues to look for compatible ones");

            Catalogues = FilterCatalogues(cataloguesToUseForImageLookup);

            Logger.Debug("Found " + Catalogues.Length + " Catalogues matching filter criteria");

            if (!Catalogues.Any())
                throw new Exception("There are no compatible Catalogues in the repository (See QueryToExecuteColumnSet for required columns)");
        }


        protected QueryToExecuteColumnSet[] FilterCatalogues(ICatalogue[] cataloguesToUseForImageLookup)
        {
            return cataloguesToUseForImageLookup.OrderBy(c => c.ID).Select(QueryToExecuteColumnSet.Create).Where(s => s != null).ToArray()!;
        }

        public IEnumerable<ExtractImageCollection> GetAllMatchingFiles(ExtractionRequestMessage message, IAuditExtractions auditor)
        {
            var queries = new List<QueryToExecute>();

            foreach (var c in GetCataloguesFor(message))
            {
                var query = GetQueryToExecute(c, message);

                if (query != null)
                {
                    queries.Add(query);
                    auditor.AuditCatalogueUse(message, c.Catalogue);
                }
            }

            Logger.Debug($"Found {queries.Count} Catalogues which support extracting based on '{message.KeyTag}'");

            if (queries.Count == 0)
                throw new Exception($"Couldn't find any compatible Catalogues to run extraction queries against for query {message}");


            foreach (string valueToLookup in message.ExtractionIdentifiers)
            {
                var results = new ExtractImageCollection(valueToLookup);

                foreach (QueryToExecute query in queries)
                {
                    foreach (QueryToExecuteResult result in query.Execute(valueToLookup, GetRejectorsFor(message, query).ToList()))
                    {
                        var seriesTagValue = result.SeriesTagValue
                            ?? throw new ArgumentNullException(nameof(result.SeriesTagValue));

                        if (!results.ContainsKey(seriesTagValue))
                            results.Add(seriesTagValue, new HashSet<QueryToExecuteResult>());

                        results[seriesTagValue].Add(result);
                    }
                }

                yield return results;
            }
        }

        public IEnumerable<IRejector> GetRejectorsFor(ExtractionRequestMessage message, QueryToExecute query)
        {
            if (message.IsNoFilterExtraction)
            {
                return Enumerable.Empty<IRejector>();
            }

            if (ModalitySpecificRejectors.Any() && string.IsNullOrWhiteSpace(query.Modality))
                throw new Exception("Could not evaluate ModalitySpecificRejectors because query Modality was null");

            var applicableRejectors =
                ModalitySpecificRejectors
                .Where(
                    // Do the modalities covered by this rejector apply to the images returned by the query
                    k => k.Key.GetModalities().Any(m => string.Equals(m, query.Modality, StringComparison.CurrentCultureIgnoreCase))
                    )
                .ToArray();

            // if modality specific rejectors override regular rejectors
            if (applicableRejectors.Any(r => r.Key.Overrides))
            {
                // they had better all override or none of them!
                if (!applicableRejectors.All(r => r.Key.Overrides))
                {
                    throw new Exception($"You cannot mix Overriding and non Overriding ModalitySpecificRejectors.  Bad Modality was '{query.Modality}'");
                }

                // yes we have custom rejection rules for this modality
                return applicableRejectors.Select(r => r.Value);
            }

            // The modality specific rejectors run in addition to the basic Rejectors so serve both
            return applicableRejectors.Select(r => r.Value).Union(Rejectors);
        }

        /// <summary>
        /// Creates a <see cref="QueryToExecute"/> based on the current <paramref name="message"/> and a <paramref name="columnSet"/> to
        /// fetch back / filter using.
        /// </summary>
        /// <param name="columnSet"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        protected virtual QueryToExecute? GetQueryToExecute(QueryToExecuteColumnSet columnSet, ExtractionRequestMessage message)
        {
            string? modality = GetModalityFor(columnSet.Catalogue);

            // do they want only records from a specific modality
            if (!string.IsNullOrWhiteSpace(message.Modalities))
            {
                if (ModalityRoutingRegex == null)
                    throw new NotSupportedException("Filtering on Modality requires setting a ModalityRoutingRegex");

                // Use Modality routing regex to identify which modality 
                var anyModality = message.Modalities.Split(',', StringSplitOptions.RemoveEmptyEntries);

                // if we know the modality
                if (modality != null)
                {
                    //but it is not one we are extracting 
                    if (!anyModality.Any(m => m.Equals(modality)))
                        return null; // i.e. do not query
                }
                else
                {
                    Logger.Log(LogLevel.Warn, nameof(ModalityRoutingRegex) + " did not match Catalogue name " + columnSet.Catalogue.Name);
                }

            }


            return new QueryToExecute(columnSet, message.KeyTag) { Modality = modality };
        }

        /// <summary>
        /// Returns the Modality of images that should be returned by querying the given <paramref name="catalogue"/>.
        /// Based on the <see cref="ModalityRoutingRegex"/> or null if unknown / mixed modalities 
        /// </summary>
        /// <param name="catalogue"></param>
        /// <returns></returns>
        private string? GetModalityFor(ICatalogue catalogue)
        {
            // We don't know how to 
            if (ModalityRoutingRegex == null)
                return null;

            var match = ModalityRoutingRegex.Match(catalogue.Name);

            return match.Success ? match.Groups[1].Value : null;

        }

        /// <summary>
        /// Return all valid query targets for the given <paramref name="message"/>.  Use this to handle throwing out queries
        /// because they go to the wrong table for the given <see cref="ExtractionRequestMessage.Modalities"/> etc.
        ///
        /// <para>Default implementation returns all <see cref="Catalogues"/> in which the <see cref="ExtractionRequestMessage.KeyTag"/>
        /// appears</para>
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        protected virtual IEnumerable<QueryToExecuteColumnSet> GetCataloguesFor(ExtractionRequestMessage message)
        {
            return Catalogues.Where(c => c.Contains(message.KeyTag));
        }

    }
}
