using System;
using System.Linq;
using Rdmp.Core.Curation.Data;
using Smi.Common.Messages.Extraction;

namespace Microservices.CohortExtractor.Execution.RequestFulfillers.Epcc
{
    public class EpccExtractionRequestFulfiller: FromCataloguesExtractionRequestFulfiller
    {
        public EpccExtractionRequestFulfiller(ICatalogue[] cataloguesToUseForImageLookup) : base(cataloguesToUseForImageLookup)
        {
        }

        protected override QueryToExecute GetQueryToExecute(QueryToExecuteColumnSet columnSet, ExtractionRequestMessage message)
        {
            //if the query is for a modality that doesn't match the Catalogue name skip it
            if (!string.IsNullOrWhiteSpace(message.Modality))
            {
                var anyModality = message.Modality.Split(',', StringSplitOptions.RemoveEmptyEntries);

                //if none of the modalities match the table name
                if(!anyModality.Any(m => columnSet.Catalogue.Name.StartsWith(m)))
                    return null;
            }

            return new QueryToExecute(columnSet, message.KeyTag);
        }
    }
}