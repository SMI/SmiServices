
using Microservices.CohortExtractor.Audit;
using Smi.Common.Messages.Extraction;
using NLog;
using Rdmp.Core.Curation.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microservices.CohortExtractor.Execution.RequestFulfillers
{
    public class FromCataloguesExtractionRequestFulfiller : IExtractionRequestFulfiller
    {
        protected readonly QueryToExecuteColumnSet[] Catalogues;
        protected readonly Logger Logger;

        public List<IRejector> Rejectors { get; set; } = new List<IRejector>();
        public Regex ModalityRoutingRegex { get; set; }

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
            return cataloguesToUseForImageLookup.OrderBy(c => c.ID).Select(QueryToExecuteColumnSet.Create).Where(s => s != null).ToArray();
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

            Logger.Debug("Found " + queries.Count + " Catalogues which support extracting based on '" + message.KeyTag + "'");

            if (queries.Count == 0)
                throw new Exception($"Couldn't find any compatible Catalogues to run extraction queries against for query {message}");

            

            foreach (string valueToLookup in message.ExtractionIdentifiers)
            {
                var results = new ExtractImageCollection(valueToLookup);

                foreach (QueryToExecute query in queries)
                {
                    foreach (var result in query.Execute(valueToLookup,Rejectors))
                    {
                        if(!results.ContainsKey(result.SeriesTagValue))
                            results.Add(result.SeriesTagValue,new HashSet<QueryToExecuteResult>());

                        results[result.SeriesTagValue].Add(result);
                    }
                }

                yield return results;
            }
        }

        /// <summary>
        /// Creates a <see cref="QueryToExecute"/> based on the current <paramref name="message"/> and a <paramref name="columnSet"/> to
        /// fetch back / filter using.
        /// </summary>
        /// <param name="columnSet"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        protected virtual QueryToExecute GetQueryToExecute(QueryToExecuteColumnSet columnSet, ExtractionRequestMessage message)
        {
            if (!string.IsNullOrWhiteSpace(message.Modality))
            {
                if(ModalityRoutingRegex == null)
                    throw new NotSupportedException("Filtering on Modality requires setting a ModalityRoutingRegex");

                var anyModality = message.Modality.Split(',', StringSplitOptions.RemoveEmptyEntries);
                var match = ModalityRoutingRegex.Match(columnSet.Catalogue.Name);

                if (match.Success)
                {
                    //if none of the modalities match the table name
                    if (!anyModality.Any(m => m.Equals(match.Groups[1].Value)))
                        return null;
                }
                else
                {
                    Logger.Log(LogLevel.Warn,nameof(ModalityRoutingRegex) + " did not match Catalogue name " + columnSet.Catalogue.Name);
                }

            }
                
            
            return new QueryToExecute(columnSet, message.KeyTag);
        }

        /// <summary>
        /// Return all valid query targets for the given <paramref name="message"/>.  Use this to handle throwing out queries
        /// because they go to the wrong table for the given <see cref="ExtractionRequestMessage.Modality"/> etc.
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