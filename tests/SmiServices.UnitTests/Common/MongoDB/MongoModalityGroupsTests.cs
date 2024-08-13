
using MongoDB.Bson;
using NUnit.Framework;
using SmiServices.Common.MongoDB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmiServices.UnitTests.Common.MongoDB
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

            Assert.That(grouped, Has.Count.EqualTo(4));
            foreach (Tuple<string, List<BsonDocument>> thing in grouped)
                Assert.That(thing.Item2, Has.Count.EqualTo(1));
        }

        [Test]
        public void ImageProcessor_ModalitySplit_NonstandardModalities()
        {
            var docs = new List<BsonDocument>
            {
                // MR group
                new() {{"tag", "value"}, {"Modality", "MR"}},
                new() {{"tag", "value"}, {"Modality", "MR"}},

                // CT group
                new() {{"tag", "value"}, {"Modality", "CT"}},
                new() {{"tag", "value"}, {"Modality", "CT"}},
                new() {{"tag", "value"}, {"Modality", "CT"}},

                // Other group
                new() {{"tag", "value"}, {"Modality", BsonNull.Value}},
                new() {{"tag", "value"}, {"Modality", "*"}},
                new() {{"tag", "value"}, {"Modality", "OTHER"}},
                new() {{"tag", "value"}}
            };

            List<Tuple<string, List<BsonDocument>>> grouped = MongoModalityGroups.GetModalityChunks(docs).ToList();

            Assert.That(grouped, Has.Count.EqualTo(3), "Expected 3 groupings");

            Assert.Multiple(() =>
            {
                Assert.That(grouped[0].Item1, Is.EqualTo("MR"), "Expected MR group");
                Assert.That(grouped[0].Item2, Has.Count.EqualTo(2), "Expected 2 in MR group");

                Assert.That(grouped[1].Item1, Is.EqualTo("CT"), "Expected CT group");
                Assert.That(grouped[1].Item2, Has.Count.EqualTo(3), "Expected 3 in CT group");

                Assert.That(grouped[2].Item1, Is.EqualTo("OTHER"), "Expected OTHER group");
                Assert.That(grouped[2].Item2, Has.Count.EqualTo(4), "Expected 4 in OTHER group");
            });
        }
    }
}
