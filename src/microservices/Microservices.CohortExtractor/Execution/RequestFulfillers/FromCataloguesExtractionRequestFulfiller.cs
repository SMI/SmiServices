
using Microservices.CohortExtractor.Audit;
using Smi.Common.Messages.Extraction;
using NLog;
using Rdmp.Core.Curation.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microservices.CohortExtractor.Execution.RequestFulfillers
{
    public class FromCataloguesExtractionRequestFulfiller : IExtractionRequestFulfiller
    {
        protected readonly QueryToExecuteColumnSet[] Catalogues;
        protected readonly Logger Logger;
        
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
                queries.Add(GetQueryToExecute(c,message));
                auditor.AuditCatalogueUse(message, c.Catalogue);
            }

            Logger.Debug("Found " + queries.Count + " Catalogues which support extracting based on '" + message.KeyTag + "'");

            if (queries.Count == 0)
                throw new Exception("Couldn't find any compatible Catalogues to run extraction queries against");

            

            foreach (string valueToLookup in message.ExtractionIdentifiers)
            {
                Dictionary<string, HashSet<QueryToExecuteResult>> results = new Dictionary<string, HashSet<QueryToExecuteResult>>();

                foreach (QueryToExecute query in queries)
                {
                    foreach (var result in query.Execute(valueToLookup))
                    {
                        if(!results.ContainsKey(result.SeriesTagValue))
                            results.Add(result.SeriesTagValue,new HashSet<QueryToExecuteResult>());

                        results[result.SeriesTagValue].Add(result);
                    }
                }

                foreach(var kvp in results)
                    yield return new ExtractImageCollection(valueToLookup,kvp.Key,new HashSet<string>(kvp.Value.Select(v=>v.FilePathValue)));
            }
        }

        protected virtual QueryToExecute GetQueryToExecute(QueryToExecuteColumnSet columnSet, ExtractionRequestMessage message)
        {
            return new QueryToExecute(columnSet, message.KeyTag);
        }

        protected virtual IEnumerable<QueryToExecuteColumnSet> GetCataloguesFor(ExtractionRequestMessage message)
        {
            return Catalogues.Where(c => c.Contains(message.KeyTag));
        }
    }
}