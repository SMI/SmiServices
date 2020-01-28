using System;
using System.Data.Common;

namespace Microservices.CohortExtractor.Execution.RequestFulfillers.Epcc
{
    public class EpccExtractionRejector : IRejector
    {
        public bool Reject(DbDataReader row, out string reason)
        {
            if (!Convert.ToBoolean(row["IsOriginal"]))
            {
                reason= "ImageType is not ORIGINAL";
                return true;
            }

            if (!Convert.ToBoolean(row["IsPrimary"]))
            {
                reason= "ImageType is not PRIMARY";
                return true;
            }

            //if the image is not extractable
            if (!Convert.ToBoolean(row["IsExtractableToDisk"]))
            {
                //tell them why and reject it
                reason = row["IsExtractableToDisk_Reason"] as string;
                return true;
            }

            reason = null;
            return false;
        }
    }
}