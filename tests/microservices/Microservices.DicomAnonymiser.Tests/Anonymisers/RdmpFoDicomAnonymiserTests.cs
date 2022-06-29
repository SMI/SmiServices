using System.IO;
using System.IO.Abstractions;
using Dicom;
using Microservices.DicomAnonymiser.Anonymisers;
using NUnit.Framework;
using Rdmp.Core.Curation.Data.Pipelines;
using Rdmp.Core.Repositories;
using Rdmp.Dicom.Extraction.FoDicomBased;
using Smi.Common.Tests;
using Tests.Common;

namespace Microservices.DicomAnonymiser.Tests.Anonymisers
{
    public class RdmpFoDicomAnonymiserTests : UnitTests
    {
        [Test]
        public void TestAnonymiseSimpleFile()
        {
            var pc = WhenIHaveA<PipelineComponent>();
            pc.Class = typeof(FoDicomAnonymiser).FullName;
            pc.SaveToDatabase();

            pc.CreateArgumentsForClassIfNotExists<FoDicomAnonymiser>();

            Repository.MEF = new MEF();

            var anonymiser = new RdmpFoDicomAnonymiser(RepositoryLocator,pc.ID);
            
            var inPath = new FileInfo(Path.Combine(TestContext.CurrentContext.WorkDirectory,"in","mydcm"));
            var outPath = new FileInfo(Path.Combine(TestContext.CurrentContext.WorkDirectory,"out","mydcm"));
            
            var fs = new FileSystem();

            TestData.Create(inPath);
            anonymiser.Anonymise(new FileInfoWrapper(fs,inPath),
            new FileInfoWrapper(fs,outPath));

            var ident = DicomFile.Open(inPath.FullName);
            var anon = DicomFile.Open(outPath.FullName);


            foreach(var tag in new []{DicomTag.PatientID})
            {
                var before = ident.Dataset.GetString(tag);
                var after = anon.Dataset.GetString(tag);

                TestContext.Out.WriteLine($"Before:{before}");
                TestContext.Out.WriteLine($"After :{after}");

                Assert.AreNotEqual(before,after);
            }
        }
    }
}