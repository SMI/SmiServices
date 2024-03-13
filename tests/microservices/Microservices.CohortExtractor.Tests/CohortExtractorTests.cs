
using Microservices.CohortExtractor.Audit;
using Microservices.CohortExtractor.Execution.RequestFulfillers;
using NUnit.Framework;
using Rdmp.Core.Curation.Data;
using Smi.Common.Helpers;
using Smi.Common.Options;
using System;
using System.Linq;
using Tests.Common;

namespace Microservices.CohortExtractor.Tests
{
    class CohortExtractorTests : UnitTests
    {
        /// <summary>
        /// Tests creating auditors and fulfillers under various conditions including the config containing only short name instead of full name.
        /// </summary>
        /// <param name="testCase"></param>
        /// <param name="fullName"></param>
        [TestCase(Test.Normal, true)]
        [TestCase(Test.Normal, false)]
        [TestCase(Test.NoAuditor, true)]
        [TestCase(Test.NoAuditor, false)]
        [TestCase(Test.NoFulfiller, true)]
        [TestCase(Test.NoFulfiller, false)]
        public void UnitTest_Reflection_AuditorAndFulfillerTypeNames(Test testCase, bool fullName)
        {
            CohortExtractorOptions opts = new();

            //override
            switch (testCase)
            {
                case Test.Normal:
                    opts.AuditorType = (fullName ? typeof(NullAuditExtractions).FullName : typeof(NullAuditExtractions).Name)!;
                    opts.RequestFulfillerType = fullName ? typeof(FromCataloguesExtractionRequestFulfiller).FullName : typeof(FromCataloguesExtractionRequestFulfiller).Name;
                    opts.Validate();
                    break;

                case Test.NoAuditor:

                    //no auditor is not a problem because it will just return NullAuditExtractions anyway
                    opts.AuditorType = "";
                    opts.RequestFulfillerType = fullName ? typeof(FromCataloguesExtractionRequestFulfiller).FullName : typeof(FromCataloguesExtractionRequestFulfiller).Name;
                    opts.Validate();
                    break;
                case Test.NoFulfiller:
                    opts.AuditorType = (fullName ? typeof(NullAuditExtractions).FullName : typeof(NullAuditExtractions).Name)!;
                    opts.RequestFulfillerType = null; //lets use null here just to cover both "" and null

                    //no fulfiller is a problem!
                    var ex = Assert.Throws<Exception>(() => opts.Validate());
                    Assert.That(ex!.Message, Does.Contain("No RequestFulfillerType set on CohortExtractorOptions"));

                    break;
                default:
                    throw new ArgumentOutOfRangeException("testCase");
            }

            //if user has not provided the full name 
            if (!fullName)
            {
                //if no auditor is provided
                if (testCase == Test.NoAuditor)
                    Assert.IsInstanceOf<NullAuditExtractions>(CreateAuditor(opts)); //this one gets created
                else
                    Assert.Throws<TypeLoadException>(() => CreateAuditor(opts)); //if an invalid auditor (not full name, we expect TypeLoadException)

                //if no fulfiller is provided
                if (testCase == Test.NoFulfiller)
                    Assert.That(CreateRequestFulfiller(opts), Is.Null); //we expect null to be returned
                else
                    Assert.Throws<TypeLoadException>(() => CreateRequestFulfiller(opts)); //if an invalid fulfiller (not full name we expect TypeLoadException)
            }
            else
            {
                Assert.That(CreateAuditor(opts), Is.Not.Null);

                if (testCase == Test.NoFulfiller)
                    Assert.That(CreateRequestFulfiller(opts), Is.Null); //we expect null to be returned
                else
                    Assert.That(CreateRequestFulfiller(opts), Is.Not.Null);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void UnitTest_Reflection_RejectorTypeNames(bool supplyRejectorName)
        {
            CohortExtractorOptions opts = new();
            opts.RequestFulfillerType = typeof(FromCataloguesExtractionRequestFulfiller).FullName;
            opts.RejectorType = supplyRejectorName ? typeof(TestRejector).FullName : null;
            opts.Validate();

            var fulfiller = CreateRequestFulfiller(opts);

            Assert.IsInstanceOf(supplyRejectorName ? typeof(TestRejector) : typeof(RejectNone), fulfiller!.Rejectors.Single());
        }

        private IExtractionRequestFulfiller? CreateRequestFulfiller(CohortExtractorOptions opts)
        {
            var c = WhenIHaveA<ExtractionInformation>().CatalogueItem.Catalogue;
            var t = c.GetTableInfoList(false).Single();
            var repo = Repository.CatalogueRepository;

            foreach (string requiredColumn in new string[]
            {
                QueryToExecuteColumnSet.DefaultImagePathColumnName,
                QueryToExecuteColumnSet.DefaultStudyIdColumnName,
                QueryToExecuteColumnSet.DefaultSeriesIdColumnName,
                QueryToExecuteColumnSet.DefaultInstanceIdColumnName,
            })
            {
                var ei = new ExtractionInformation(repo, new CatalogueItem(repo, c, "a"), new ColumnInfo(repo, requiredColumn, "varchar(10)", (TableInfo)t), requiredColumn);
                ei.ExtractionCategory = ExtractionCategory.Core;
                ei.SaveToDatabase();
            }

            c.ClearAllInjections();

            Assert.That(c.GetAllExtractionInformation(ExtractionCategory.Any), Has.Length.EqualTo(5));

            var f = new MicroserviceObjectFactory();
            var fulfiller = f.CreateInstance<IExtractionRequestFulfiller>(opts.RequestFulfillerType!,
                typeof(IExtractionRequestFulfiller).Assembly,
                new object[] { new[] { c } });

            if (fulfiller != null)
                fulfiller.Rejectors.Add(f.CreateInstance<IRejector>(opts.RejectorType!, typeof(TestRejector).Assembly) ?? new RejectNone());

            return fulfiller;
        }

        private IAuditExtractions? CreateAuditor(CohortExtractorOptions opts)
        {
            var f = new MicroserviceObjectFactory();
            return f.CreateInstance<IAuditExtractions>(opts.AuditorType, typeof(IAuditExtractions).Assembly);
        }

        internal enum Test
        {
            Normal,
            NoAuditor,
            NoFulfiller,
        }
    }

}
