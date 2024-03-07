using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FellowOakDicom;
using FAnsi.Implementation;
using FAnsi.Implementations.MicrosoftSQL;
using NUnit.Framework;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.DataLoad;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.Repositories;
using Rdmp.Core.ReusableLibraryCode.Progress;
using Rdmp.Dicom.PipelineComponents.DicomSources;
using Rdmp.Dicom.PipelineComponents.DicomSources.Worklists;

namespace Microservices.Tests.RDMPTests
{
    public class DicomDatasetCollectionSourceTests
    {
        [OneTimeSetUp]
        public void InitializeFansi()
        {
            ImplementationManager.Load<MicrosoftSQLImplementation>();
        }

        /// <summary>
        /// Demonstrates the basic scenario in which a dicom dataset is turned into a data table by the DicomDatasetCollectionSource
        /// </summary>
        [Test]
        public void SourceReadSimpleTagToTable()
        {
            var source = new DicomDatasetCollectionSource();

            var ds = new DicomDataset();
            ds.Add(DicomTag.PatientAge, "123Y");

            var worklist = new ExplicitListDicomDatasetWorklist(new []{ds},"fish.dcm");

            source.PreInitialize(worklist,ThrowImmediatelyDataLoadEventListener.Quiet);
            source.FilenameField = "RelFileName";

            var dt = source.GetChunk(ThrowImmediatelyDataLoadEventListener.Quiet, new GracefulCancellationToken());
            Assert.AreEqual("123Y", dt.Rows[0]["PatientAge"]);
            Assert.AreEqual("fish.dcm", dt.Rows[0]["RelFileName"]);
        }


        [TestCase(DataTooWideHandling.None)]
        [TestCase(DataTooWideHandling.TruncateAndWarn)]
        [TestCase(DataTooWideHandling.MarkCorrupt)]
        [TestCase(DataTooWideHandling.ConvertToNullAndWarn)]
        public void TestStringTooLong(DataTooWideHandling strategy)
        {
            var ds = new DicomDataset();

#pragma warning disable CS0618 // Obsolete
            ds.AutoValidate = false;
#pragma warning restore CS0618

            ds.AddOrUpdate(DicomTag.AccessionNumber, "1342340123129473279427572495349757459347839479375974");
            ds.GetValues<string>(DicomTag.AccessionNumber);

            var source = new DicomDatasetCollectionSource();

            var worklist = new ExplicitListDicomDatasetWorklist(new[] {ds}, "fish.dcm", new Dictionary<string, string>());
            source.DataTooLongHandlingStrategy = strategy;
            source.FilenameField = "abc";
            source.PreInitialize(worklist,ThrowImmediatelyDataLoadEventListener.Quiet);

            var dt = source.GetChunk(ThrowImmediatelyDataLoadEventListener.Quiet, new GracefulCancellationToken());

            switch (strategy)
            {
                case DataTooWideHandling.None:
                    Assert.AreEqual("1342340123129473279427572495349757459347839479375974", dt.Rows[0]["AccessionNumber"]);
                    Assert.AreEqual(0, worklist.CorruptMessages.Count);
                    break;
                case DataTooWideHandling.TruncateAndWarn:
                    Assert.AreEqual("1342340123129473", dt.Rows[0]["AccessionNumber"]);
                    Assert.AreEqual(0, worklist.CorruptMessages.Count);
                    break;
                case DataTooWideHandling.MarkCorrupt:
                    Assert.IsNull(dt); //since dt has no rows it just returns null
                    Assert.AreEqual(1,worklist.CorruptMessages.Count);
                    break;
                case DataTooWideHandling.ConvertToNullAndWarn:
                    Assert.AreEqual(DBNull.Value, dt.Rows[0]["AccessionNumber"]);
                    Assert.AreEqual(0, worklist.CorruptMessages.Count);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(strategy));
            }
        }

        /// <summary>
        /// Demonstrates that invalid float values are not a problem and get deserialized as strings
        /// </summary>
        [TestCase(InvalidDataHandling.ConvertToNullAndWarn)]
        [TestCase(InvalidDataHandling.ThrowException)]
        public void SourceRead_InvalidFloat_ToTable(InvalidDataHandling dataHandlingStrategy)
        {
            var source = new DicomDatasetCollectionSource
            {
                InvalidDataHandlingStrategy = dataHandlingStrategy
            };

            var ds = new DicomDataset();
            ds.Add(DicomTag.PatientAge, "123Y");
            ds.Add(DicomTag.WedgeAngleFloat, "3.40282347e+038");

            var worklist = new ExplicitListDicomDatasetWorklist(new[] { ds }, "fish.dcm",new Dictionary<string, string> { {"MessageGuid", "123x321" } });

            source.PreInitialize(worklist, ThrowImmediatelyDataLoadEventListener.Quiet);
            source.FilenameField = "RelFileName";

            DataTable? dt = null;

            switch (dataHandlingStrategy)
            {
                case InvalidDataHandling.ThrowException:
                    Assert.Throws<ArgumentException>(()=>source.GetChunk(ThrowImmediatelyDataLoadEventListener.Quiet, new GracefulCancellationToken()));
                    return;

                case InvalidDataHandling.ConvertToNullAndWarn:
                    var toMem = new ToMemoryDataLoadEventListener(true);
                    dt = source.GetChunk(toMem, new GracefulCancellationToken());

                    Assert.AreEqual(DBNull.Value, dt.Rows[0]["WedgeAngleFloat"]);

                    //should be a warning about WedgeAngleFloat logged
                    var warning = toMem.EventsReceivedBySender.SelectMany(static e => e.Value).Single(v => v.ProgressEventType == ProgressEventType.Warning);
                    Assert.IsTrue(warning.Message.Contains("WedgeAngleFloat"));
                    Assert.IsTrue(warning.Message.Contains("MessageGuid"));
                    Assert.IsTrue(warning.Message.Contains("123x321"));
                    Assert.IsTrue(warning.Message.Contains("fish.dcm"));

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(dataHandlingStrategy));
            }

            Assert.AreEqual("123Y", dt.Rows[0]["PatientAge"]);
            Assert.AreEqual("fish.dcm", dt.Rows[0]["RelFileName"]);
        }

        [TestCase(InvalidDataHandling.ConvertToNullAndWarn)]
        [TestCase(InvalidDataHandling.ThrowException)]
        [TestCase(InvalidDataHandling.MarkCorrupt)]
        public void SourceRead_InvalidFloatInSequence_ToTable(InvalidDataHandling dataHandlingStrategy)
        {

            var source = new DicomDatasetCollectionSource
            {
                InvalidDataHandlingStrategy = dataHandlingStrategy
            };

            //when we have a dicom file with an invalid Float number
            var ds = new DicomDataset();
            ds.Add(DicomTag.PatientAge, "123Y");

            var sequence = new DicomSequence(DicomTag.AcquisitionContextSequence,
                new DicomDataset
                {
                    {DicomTag.WedgeAngleFloat, "3.40282347e+038"}
                });

            ds.Add(sequence);

            var worklist = new ExplicitListDicomDatasetWorklist(new[] { ds }, "fish.dcm");

            source.PreInitialize(worklist, ThrowImmediatelyDataLoadEventListener.Quiet);
            source.FilenameField = "RelFileName";

            DataTable? dt = null;

            switch (dataHandlingStrategy)
            {

                case InvalidDataHandling.MarkCorrupt:
                    dt =source.GetChunk(ThrowImmediatelyDataLoadEventListener.Quiet, new GracefulCancellationToken());

                    //row was not processed (which leaves data table with 0 rows and hence component returns null)
                    Assert.IsNull(dt);

                    //corrupt message should appear in the worklist
                    Assert.AreEqual(1,worklist.CorruptMessages.Count);
                    return;
                case InvalidDataHandling.ConvertToNullAndWarn:
                    dt = source.GetChunk(ThrowImmediatelyDataLoadEventListener.Quiet, new GracefulCancellationToken());

                    Assert.AreEqual("123Y", dt.Rows[0]["PatientAge"]);
                    Assert.AreEqual("fish.dcm", dt.Rows[0]["RelFileName"]);
                    Assert.AreEqual(DBNull.Value,dt.Rows[0]["AcquisitionContextSequence"]);
                    Assert.AreEqual(0,worklist.CorruptMessages.Count);
                    break;
                case InvalidDataHandling.ThrowException:
                    Assert.Throws<ArgumentException>(() => source.GetChunk(ThrowImmediatelyDataLoadEventListener.Quiet, new GracefulCancellationToken()));
                    return;

                default:
                    throw new ArgumentOutOfRangeException("dataHandlingStrategy");
            }

        }

        [TestCase(InvalidDataHandling.ConvertToNullAndWarn)]
        [TestCase(InvalidDataHandling.ThrowException)]
        public void SourceRead_InvalidFloatInSequence_WithElevation_ToTable(InvalidDataHandling dataHandlingStrategy)
        {
            //create the elevation configuration
            var elevationRules = new FileInfo(Path.Combine(TestContext.CurrentContext.TestDirectory,"ElevationConfig.xml"));

            File.WriteAllText(elevationRules.FullName,
@"<!DOCTYPE TagElevationRequestCollection
[
  <!ELEMENT TagElevationRequestCollection (TagElevationRequest*)>
  <!ELEMENT TagElevationRequest (ColumnName,ElevationPathway,Conditional?)>
  <!ELEMENT ColumnName (#PCDATA)>
  <!ELEMENT ElevationPathway (#PCDATA)>
  <!ELEMENT Conditional (ConditionalPathway,ConditionalRegex)>
  <!ELEMENT ConditionalPathway (#PCDATA)>
  <!ELEMENT ConditionalRegex (#PCDATA)>
]>
<TagElevationRequestCollection>
  <TagElevationRequest>
    <ColumnName>WedgeAngleFloat</ColumnName>
    <ElevationPathway>AcquisitionContextSequence->WedgeAngleFloat</ElevationPathway>
  </TagElevationRequest>
</TagElevationRequestCollection>");

            //setup the source reader
            var source = new DicomDatasetCollectionSource();
            source.InvalidDataHandlingStrategy = dataHandlingStrategy;
            source.TagElevationConfigurationFile = elevationRules;

            //don't load the sequence, just the elevation
            source.TagBlacklist = new Regex("AcquisitionContextSequence");

            //The dataset we are trying to load
            var ds = new DicomDataset();
            ds.Add(DicomTag.PatientAge, "123Y");

            var sequence = new DicomSequence(DicomTag.AcquisitionContextSequence,
                new DicomDataset
                {
                    {DicomTag.WedgeAngleFloat, "3.40282347e+038"} //dodgy float in sequence (the sequence we are trying to elevate)
                });

            ds.Add(sequence);

            var worklist = new ExplicitListDicomDatasetWorklist(new[] { ds }, "fish.dcm", new Dictionary<string, string> { { "MessageGuid", "123x321" } });

            source.PreInitialize(worklist, ThrowImmediatelyDataLoadEventListener.Quiet);
            source.FilenameField = "RelFileName";

            DataTable? dt = null;

            switch (dataHandlingStrategy)
            {
                case InvalidDataHandling.ThrowException:
                    Assert.Throws<ArgumentException>(() => source.GetChunk(ThrowImmediatelyDataLoadEventListener.Quiet, new GracefulCancellationToken()));
                    return;

                case InvalidDataHandling.ConvertToNullAndWarn:
                    var tomem = new ToMemoryDataLoadEventListener(true);
                    dt = source.GetChunk(tomem, new GracefulCancellationToken());
                    Assert.AreEqual(DBNull.Value, dt.Rows[0]["WedgeAngleFloat"]);

                    //should be a warning about WedgeAngleFloat logged
                    var warning = tomem.EventsReceivedBySender.SelectMany(e => e.Value).Single(v => v.ProgressEventType == ProgressEventType.Warning);
                    Assert.IsTrue(warning.Message.Contains("WedgeAngleFloat"));
                    Assert.IsTrue(warning.Message.Contains("MessageGuid"));
                    Assert.IsTrue(warning.Message.Contains("123x321"));
                    Assert.IsTrue(warning.Message.Contains("fish.dcm"));

                    break;

                default:
                    throw new ArgumentOutOfRangeException("dataHandlingStrategy");
            }

            Assert.AreEqual("123Y", dt.Rows[0]["PatientAge"]);
            Assert.AreEqual("fish.dcm", dt.Rows[0]["RelFileName"]);
        }

        [Test]
        public void SourceRead_ToTable_IgnoringSuperflousColumn_TableInfo()
        {
            var repo = new MemoryCatalogueRepository();

            var ti = new TableInfo(repo, "MyTable");
            new ColumnInfo(repo, "PatientAge", "varchar(100)", ti);
            new ColumnInfo(repo, "RelFileName", "varchar(100)", ti);

            var source = new DicomDatasetCollectionSource();
            source.InvalidDataHandlingStrategy = InvalidDataHandling.ThrowException;

            var ds = new DicomDataset();
            ds.Add(DicomTag.PatientAge, "123Y");

            var sequence = new DicomSequence(DicomTag.AcquisitionContextSequence,
                new DicomDataset
                {
                    {DicomTag.WedgeAngleFloat, "3.40282347e+038"}
                });

            ds.Add(sequence);

            var worklist = new ExplicitListDicomDatasetWorklist(new[] { ds }, "fish.dcm");

            source.PreInitialize(worklist, ThrowImmediatelyDataLoadEventListener.Quiet);
            source.FilenameField = "RelFileName";
            source.FieldMapTableIfAny = ti;

            var dt = source.GetChunk(ThrowImmediatelyDataLoadEventListener.Quiet, new GracefulCancellationToken());
            Assert.AreEqual("123Y", dt.Rows[0]["PatientAge"]);
            Assert.AreEqual("fish.dcm", dt.Rows[0]["RelFileName"]);
            Assert.AreEqual(2,dt.Columns.Count);
        }

        [Test]
        public void SourceRead_ToTable_IgnoringSuperflousColumn_LoadMetadata()
        {
            var repo = new MemoryCatalogueRepository();

            var lmd = new LoadMetadata(repo, "MyLoad");

            var cata1 = new Catalogue(repo, "PatientCatalogue");
            var ci1 = new CatalogueItem(repo, cata1, "PatientAge");
            var ti1 = new TableInfo(repo, "PatientTableInfo");
            var colInfo1 = new ColumnInfo(repo, "PatientAge", "varchar(100)", ti1);
            ci1.ColumnInfo_ID = colInfo1.ID;
            ci1.SaveToDatabase();
            cata1.LoadMetadata_ID = lmd.ID;
            cata1.SaveToDatabase();

            var cata2 = new Catalogue(repo, "FileCatalogue");
            var ci2 = new CatalogueItem(repo, cata2, "RelFileName");
            var ti2 = new TableInfo(repo, "FileTableInfo");
            var colInfo2 = new ColumnInfo(repo, "RelFileName", "varchar(100)", ti2);
            ci2.ColumnInfo_ID = colInfo2.ID;
            ci2.SaveToDatabase();
            cata2.LoadMetadata_ID = lmd.ID;
            cata2.SaveToDatabase();

            var source = new DicomDatasetCollectionSource();
            source.InvalidDataHandlingStrategy = InvalidDataHandling.ThrowException;

            var ds = new DicomDataset();
            ds.Add(DicomTag.PatientAge, "123Y");

            var sequence = new DicomSequence(DicomTag.AcquisitionContextSequence,
                new DicomDataset
                {
                    {DicomTag.WedgeAngleFloat, "3.40282347e+038"}
                });

            ds.Add(sequence);

            var worklist = new ExplicitListDicomDatasetWorklist(new[] { ds }, "fish.dcm");

            source.PreInitialize(worklist, ThrowImmediatelyDataLoadEventListener.Quiet);
            source.FilenameField = "RelFileName";
            source.UseAllTableInfoInLoadAsFieldMap = lmd;

            var dt = source.GetChunk(ThrowImmediatelyDataLoadEventListener.Quiet, new GracefulCancellationToken());
            Assert.AreEqual("123Y", dt.Rows[0]["PatientAge"]);
            Assert.AreEqual("fish.dcm", dt.Rows[0]["RelFileName"]);
            Assert.AreEqual(2, dt.Columns.Count);
        }
    }
}
