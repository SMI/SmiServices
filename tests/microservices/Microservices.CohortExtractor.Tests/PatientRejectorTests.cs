using FAnsi;
using FAnsi.Discovery;
using Microservices.CohortExtractor.Execution.RequestFulfillers;
using Moq;
using NUnit.Framework;
using Rdmp.Core.Curation;
using Rdmp.Core.Curation.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using Tests.Common;

namespace Microservices.CohortExtractor.Tests
{
    class PatientRejectorTests : DatabaseTests
    {
        [TestCase(DatabaseType.MicrosoftSQLServer)]
        [TestCase(DatabaseType.MySql)]
        public void TestRejectPatients(DatabaseType type)
        {
            var server = GetCleanedServer(type);

            var tbl = server.CreateTable("BadPatients",new []{new DatabaseColumnRequest("Pat","varchar(100)") });
            
            tbl.Insert(new Dictionary<string, object>(){{"Pat","Frank"} });
            tbl.Insert(new Dictionary<string, object>(){{"Pat","Peter" } });
            tbl.Insert(new Dictionary<string, object>(){{"Pat","Frank" } }); //duplication for the lols
            tbl.Insert(new Dictionary<string, object>(){{"Pat","David" } });

            new TableInfoImporter(CatalogueRepository,tbl).DoImport(out var ti,out var cols);
            
            var rejector = new PatientRejector(cols[0]);

            var moqDave = new Mock<DbDataReader>();
            moqDave.Setup(x => x["PatientId"])
                .Returns("Dave");

            Assert.IsFalse(rejector.Reject(moqDave.Object,out string reason));
            Assert.IsNull(reason);


            var moqFrank = new Mock<DbDataReader>();
            moqFrank.Setup(x => x["PatientId"])
                .Returns("Frank");

            Assert.IsTrue(rejector.Reject(moqFrank.Object,out reason));
            Assert.AreEqual("Patient was in reject list",reason);

            var moqLowerCaseFrank = new Mock<DbDataReader>();
            moqLowerCaseFrank.Setup(x => x["PatientId"])
                .Returns("frank");

            Assert.IsTrue(rejector.Reject(moqLowerCaseFrank.Object,out  reason));
            Assert.AreEqual("Patient was in reject list",reason);





        }
    }
}
