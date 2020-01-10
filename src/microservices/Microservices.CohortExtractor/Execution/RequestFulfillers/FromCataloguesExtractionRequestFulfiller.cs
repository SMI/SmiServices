
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
        public const string ImagePathColumnName = "RelativeFileArchiveURI";
        public const string SeriesIdColumnName = "SeriesInstanceUID";


        private readonly Dictionary<ICatalogue, ExtractionInformation[]> _catalogues;
        private readonly Logger _logger;


        public FromCataloguesExtractionRequestFulfiller(ICatalogue[] cataloguesToUseForImageLookup)
        {
            _logger = LogManager.GetCurrentClassLogger();

            _logger.Debug("Preparing to filter " + cataloguesToUseForImageLookup.Length + " Catalogues to look for compatible ones");

            _catalogues = FilterCatalogues(cataloguesToUseForImageLookup);

            _logger.Debug("Found " + _catalogues.Count + " Catalogues containing an extractable field '" + ImagePathColumnName + "'");

            if (!_catalogues.Any())
                throw new Exception("There are no compatible Catalogues in the repository, there must be at least one Catalogue with an extractable column called '" + ImagePathColumnName + "' (ExtractionInformation)");
        }


        private Dictionary<ICatalogue, ExtractionInformation[]> FilterCatalogues(ICatalogue[] cataloguesToUseForImageLookup)
        {
            var toReturn = new Dictionary<ICatalogue, ExtractionInformation[]>();

            foreach (ICatalogue cata in cataloguesToUseForImageLookup.OrderBy(c => c.ID))
            {
                ExtractionInformation[] eis = cata.GetAllExtractionInformation(ExtractionCategory.Any);

                // If the Catalogue does not expose the image path ignore it
                if (!eis.Any(e => e.GetRuntimeName().Equals(ImagePathColumnName, StringComparison.CurrentCultureIgnoreCase)))
                    continue;

                // It does expose the image path
                toReturn.Add(cata, eis);
            }

            return toReturn;
        }

        public IEnumerable<ExtractImageCollection> GetAllMatchingFiles(ExtractionRequestMessage message, IAuditExtractions auditor)
        {
            var queries = new List<QueryToExecute>();

            foreach (KeyValuePair<ICatalogue, ExtractionInformation[]> eis in _catalogues)
            {
                ExtractionInformation keyTagColumn = eis.Value.SingleOrDefault(ei => ei.GetRuntimeName().Equals(message.KeyTag, StringComparison.CurrentCultureIgnoreCase));
                ExtractionInformation filePathColumn = eis.Value.SingleOrDefault(ei => ei.GetRuntimeName().Equals(ImagePathColumnName, StringComparison.CurrentCultureIgnoreCase));
                ExtractionInformation seriesTagColumn = eis.Value.SingleOrDefault(ei => ei.GetRuntimeName().Equals(SeriesIdColumnName));

                if (filePathColumn != null && keyTagColumn != null && seriesTagColumn != null)
                    queries.Add(new QueryToExecute(eis.Key, keyTagColumn, filePathColumn, seriesTagColumn));
                
                auditor.AuditCatalogueUse(message, eis.Key);
            }

            _logger.Debug("Found " + queries.Count + " Catalogues with columns called '" + ImagePathColumnName + "' and '" + message.KeyTag + "'");

            if (queries.Count == 0)
                throw new Exception("Couldn't find any compatible Catalogues to run extraction queries against");

            foreach (string valueToLookup in message.ExtractionIdentifiers)
            {
                var current = new HashSet<string>();
                string seriesInstanceUid = null;

                if (message.KeyTag == SeriesIdColumnName)
                    seriesInstanceUid = valueToLookup;

                foreach (QueryToExecute query in queries)
                {
                    // If extracting by SopId, the HashSet should only contain 1 value?
                    Tuple<string, HashSet<string>> results = query.Execute(valueToLookup);

                    if(seriesInstanceUid == null)
                        seriesInstanceUid = results.Item1;

                    foreach (string s in results.Item2)
                        current.Add(s);
                }

                yield return new ExtractImageCollection(valueToLookup, seriesInstanceUid, current);
            }
        }
    }
}