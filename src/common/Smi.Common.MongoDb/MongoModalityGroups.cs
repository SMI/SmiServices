
using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;


namespace Smi.Common.MongoDB
{
    public static class MongoModalityGroups
    {
        /// <summary>
        /// Modalities which are grouped into their own collections in MongoDB
        /// </summary>
        public static readonly string[] MajorModalities =
        {
            "CR",
            "CT",
            "DX",
            "MG",
            "MR",
            "NM",
            "OT",
            "PR",
            "PT",
            "RF",
            "SR",
            "US",
            "XA",
        };

        /// <summary>
        /// Splits a collection of Dicom Bson documents into groups determined by their Modality.
        /// Groups are defined by the <see cref="MajorModalities"/> array.
        /// </summary>
        /// <param name="toProcess"></param>
        /// <returns></returns>
        public static IEnumerable<Tuple<string, List<BsonDocument>>> GetModalityChunks(IEnumerable<BsonDocument> toProcess)
        {
            ILookup<bool, BsonDocument> areInvalid = toProcess.ToLookup(x => !x.Contains("Modality") || x["Modality"].IsBsonNull);

            // Pull out nulls first
            List<BsonDocument> others = areInvalid[true].ToList();

            foreach (IGrouping<string, BsonDocument> grouping in areInvalid[false].GroupBy(x => x["Modality"].AsString))
            {
                List<BsonDocument> groupDocs = grouping.ToList();

                if (MajorModalities.Contains(grouping.Key))
                    yield return new Tuple<string, List<BsonDocument>>(grouping.Key, groupDocs);
                else
                    others.AddRange(groupDocs);
            }

            if (others.Count > 0)
                yield return new Tuple<string, List<BsonDocument>>("OTHER", others);
        }
    }
}
