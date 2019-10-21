using Microservices.CohortExtractor.Audit;
using Microservices.CohortExtractor.Execution.RequestFulfillers;
using Microservices.Common.Options;
using NUnit.Framework;
using System;
using Microservices.Common.Helpers;
using Tests.Common;
using Rdmp.Core.Curation.Data;

namespace Microservices.Tests.RDMPTests.CohortExtractorTests
{
    class CohortExtractorTests:UnitTests
    {
        /// <summary>
        /// Tests creating auditors and fulfillers under various conditions including the config containing only short name instead of full name.
        /// </summary>
        /// <param name="testCase"></param>
        /// <param name="fullName"></param>
        [TestCase(Test.Normal,true)]
        [TestCase(Test.Normal,false)]
        [TestCase(Test.NoAuditor,true)]
        [TestCase(Test.NoAuditor,false)]
        [TestCase(Test.NoFulfiller,true)]
        [TestCase(Test.NoFulfiller,false)]
        public void UnitTest_Reflection_AuditorAndFulfillerTypeNames(Test testCase,bool fullName)
        {
            CohortExtractorOptions opts = new CohortExtractorOptions();

            //override
            switch (testCase)
            {
                case Test.Normal:
                    opts.AuditorType = fullName ? typeof(NullAuditExtractions).FullName:typeof(NullAuditExtractions).Name;
                    opts.RequestFulfillerType = fullName ? typeof(FromCataloguesExtractionRequestFulfiller).FullName:typeof(FromCataloguesExtractionRequestFulfiller).Name;
                    opts.Validate();
                    break;

                case Test.NoAuditor:
                    
                    //no auditor is not a problem because it will just return NullAuditExtractions anyway
                    opts.AuditorType = "";
                    opts.RequestFulfillerType = fullName ? typeof(FromCataloguesExtractionRequestFulfiller).FullName:typeof(FromCataloguesExtractionRequestFulfiller).Name;
                    opts.Validate();
                    break;
                case Test.NoFulfiller:
                    opts.AuditorType = fullName ? typeof(NullAuditExtractions).FullName:typeof(NullAuditExtractions).Name;
                    opts.RequestFulfillerType = null; //lets use null here just to cover both "" and null
                    
                    //no fulfiller is a problem!
                    var ex = Assert.Throws<Exception>(()=>opts.Validate());
                    StringAssert.Contains("No RequestFulfillerType set on CohortExtractorOptions",ex.Message);

                    break;
                default:
                    throw new ArgumentOutOfRangeException("testCase");
            }

            //if user has not provided the full name 
            if (!fullName)
            {
                //if no auditor is provided
                if(testCase == Test.NoAuditor)
                    Assert.IsInstanceOf<NullAuditExtractions>(CreateAuditor(opts)); //this one gets created
                else
                    Assert.Throws<TypeLoadException>(() => CreateAuditor(opts)); //if an invalid auditor (not full name, we expect TypeLoadException)

                //if no fulfiller is provided
                if (testCase == Test.NoFulfiller)
                    Assert.IsNull(CreateRequestFulfiller(opts)); //we expect null to be returned
                else
                    Assert.Throws<TypeLoadException>(() => CreateRequestFulfiller(opts)); //if an invalid fulfiller (not full name we expect TypeLoadException)
            }
            else
            {
                Assert.IsNotNull(CreateAuditor(opts));

                if (testCase == Test.NoFulfiller)
                    Assert.IsNull(CreateRequestFulfiller(opts)); //we expect null to be returned
                else
                    Assert.IsNotNull(CreateRequestFulfiller(opts));
            }
        }

        private IExtractionRequestFulfiller CreateRequestFulfiller(CohortExtractorOptions opts)
        {
            var ei = WhenIHaveA<ExtractionInformation>();
            ei.Alias = "RelativeFileArchiveURI";
            ei.SaveToDatabase();

            var f = new MicroserviceObjectFactory();

            var catas = new[]{ei.CatalogueItem.Catalogue};
            return f.CreateInstance<IExtractionRequestFulfiller>(opts.RequestFulfillerType,
                typeof(IExtractionRequestFulfiller).Assembly,
                new object[] {catas});

        }

        private IAuditExtractions CreateAuditor(CohortExtractorOptions opts)
        {
            var f = new MicroserviceObjectFactory();
            return f.CreateInstance<IAuditExtractions>(opts.AuditorType,typeof(IAuditExtractions).Assembly);
        }

        internal enum Test
        {
            Normal,
            NoAuditor,
            NoFulfiller,
        }
    }
    
}
