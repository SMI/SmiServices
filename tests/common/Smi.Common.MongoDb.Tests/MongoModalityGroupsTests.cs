
using MongoDB.Bson;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Smi.Common.MongoDB.Tests
{
    [TestFixture]
    public class MongoModalityGroupsTests
    {
        [Test]
        public void ImageProcessor_ModalitySplit_StandardModalities()
        {
            List<BsonDocument> docs = MongoModalityGroups.MajorModalities
                .Take(4)
                .Select(x => new BsonDocument { { "tag", "value" }, { "Modality", x } })
                .ToList();

            List<Tuple<string, List<BsonDocument>>> grouped = MongoModalityGroups.GetModalityChunks(docs).ToList();

            Assert.AreEqual(4, grouped.Count);
            foreach (Tuple<string, List<BsonDocument>> thing in grouped)
                Assert.AreEqual(1, thing.Item2.Count);
        }

        [Test]
        public void ImageProcessor_ModalitySplit_NonstandardModalities()
        {
            var docs = new List<BsonDocument>
            {
                // MR group
                new BsonDocument {{"tag", "value"}, {"Modality", "MR"}},
                new BsonDocument {{"tag", "value"}, {"Modality", "MR"}},

                // CT group
                new BsonDocument {{"tag", "value"}, {"Modality", "CT"}},
                new BsonDocument {{"tag", "value"}, {"Modality", "CT"}},
                new BsonDocument {{"tag", "value"}, {"Modality", "CT"}},

                // Other group
                new BsonDocument {{"tag", "value"}, {"Modality", BsonNull.Value}},
                new BsonDocument {{"tag", "value"}, {"Modality", "*"}},
                new BsonDocument {{"tag", "value"}, {"Modality", "OTHER"}},
                new BsonDocument {{"tag", "value"}}
            };

            List<Tuple<string, List<BsonDocument>>> grouped = MongoModalityGroups.GetModalityChunks(docs).ToList();

            Assert.AreEqual(3, grouped.Count, "Expected 3 groupings");

            Assert.AreEqual("MR", grouped[0].Item1, "Expected MR group");
            Assert.AreEqual(2, grouped[0].Item2.Count, "Expected 2 in MR group");

            Assert.AreEqual("CT", grouped[1].Item1, "Expected CT group");
            Assert.AreEqual(3, grouped[1].Item2.Count, "Expected 3 in CT group");

            Assert.AreEqual("OTHER", grouped[2].Item1, "Expected OTHER group");
            Assert.AreEqual(4, grouped[2].Item2.Count, "Expected 4 in OTHER group");
        }
    }
}
