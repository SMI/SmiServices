
using FellowOakDicom;
using DicomTypeTranslation;
using DicomTypeTranslation.TableCreation;
using FAnsi.Discovery;
using NUnit.Framework;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.DataLoad;
using Rdmp.Core.Curation.Data.Pipelines;
using Rdmp.Core.Repositories;
using Rdmp.Dicom.CommandExecution;
using Rdmp.Dicom.PipelineComponents.DicomSources;
using System;
using System.IO;
using System.Linq;
using Rdmp.Core.MapsDirectlyToDatabaseTable;
using Rdmp.Core.ReusableLibraryCode.Checks;
using Rdmp.Dicom.TagPromotionSchema;
using SmiServices.Common.Messages;
using SmiServices.Common.Options;

namespace SmiServices.UnitTests.Microservices.DicomRelationalMapper
{
    public class DicomRelationalMapperTestHelper
    {
        public LoadMetadata? LoadMetadata { get; private set; }
        public DiscoveredTable? ImageTable { get; private set; }
        public DiscoveredTable? SeriesTable { get; private set; }
        public DiscoveredTable? StudyTable { get; private set; }

        public TableInfo? ImageTableInfo { get; private set; }
        public TableInfo? SeriesTableInfo { get; private set; }
        public TableInfo? StudyTableInfo { get; private set; }

        public PipelineComponent? DicomSourcePipelineComponent { get; private set; }

        public void SetupSuite(DiscoveredDatabase databaseToCreateInto, IRDMPPlatformRepositoryServiceLocator repositoryLocator, GlobalOptions globalOptions, Type pipelineDicomSourceType, string? root = null, ImageTableTemplateCollection? template = null, bool persistentRaw = false, string? modalityPrefix = null)
        {
            ImageTable = databaseToCreateInto.ExpectTable($"{modalityPrefix}ImageTable");
            SeriesTable = databaseToCreateInto.ExpectTable($"{modalityPrefix}SeriesTable");
            StudyTable = databaseToCreateInto.ExpectTable($"{modalityPrefix}StudyTable");

            try
            {
                var dest = Path.Combine(TestContext.CurrentContext.TestDirectory, "Rdmp.Dicom.dll");
                if (!File.Exists(dest))
                    File.Copy(typeof(InvalidDataHandling).Assembly.Location, dest, false);
            }
            catch (IOException)
            {
                //never mind, it's probably locked
            }

            var catalogueRepository = repositoryLocator.CatalogueRepository;
            var dataExportRepository = repositoryLocator.DataExportRepository;

            foreach (var t in new[] { ImageTable, SeriesTable, StudyTable })
                if (t.Exists())
                    t.Drop();

            // delete any remnants
            foreach (var p in catalogueRepository.GetAllObjects<Pipeline>())
            {
                if (p.Name.Contains("(Image Loading Pipe)"))
                {
                    p.DeleteInDatabase();
                }
            }

            var suite = new ExecuteCommandCreateNewImagingDatasetSuite(repositoryLocator, databaseToCreateInto, new DirectoryInfo(TestContext.CurrentContext.TestDirectory))
            {
                Template = template ?? GetDefaultTemplate(databaseToCreateInto.Server.DatabaseType),

                PersistentRaw = persistentRaw,
                TablePrefix = modalityPrefix,

                DicomSourceType = pipelineDicomSourceType,
                CreateCoalescer = true
            };

            suite.Execute();
            DicomSourcePipelineComponent = suite.DicomSourcePipelineComponent; //store the component created so we can inject/adjust the arguments e.g. adding ElevationRequests to it

            LoadMetadata = suite.NewLoadMetadata;


            var tableInfos = LoadMetadata.GetAllCatalogues().SelectMany(c => c.GetTableInfoList(false)).Distinct().ToArray();

            ImageTableInfo = (TableInfo)tableInfos.Single(t => t.GetRuntimeName()?.Equals(ImageTable.GetRuntimeName()) == true);
            SeriesTableInfo = (TableInfo)tableInfos.Single(t => t.GetRuntimeName()?.Equals(SeriesTable.GetRuntimeName()) == true);
            StudyTableInfo = (TableInfo)tableInfos.Single(t => t.GetRuntimeName()?.Equals(StudyTable.GetRuntimeName()) == true);

            // Override the options with stuff coming from Core RDMP DatabaseTests (TestDatabases.txt)
            globalOptions.FileSystemOptions!.FileSystemRoot = root ?? TestContext.CurrentContext.TestDirectory;

            globalOptions.RDMPOptions!.CatalogueConnectionString = (catalogueRepository as TableRepository)?.DiscoveredServer.Builder.ConnectionString;
            globalOptions.RDMPOptions.DataExportConnectionString = (dataExportRepository as TableRepository)?.DiscoveredServer.Builder.ConnectionString;

            globalOptions.DicomRelationalMapperOptions!.LoadMetadataId = LoadMetadata.ID;
            globalOptions.DicomRelationalMapperOptions.MinimumBatchSize = 1;
            globalOptions.DicomRelationalMapperOptions.UseInsertIntoForRAWMigration = true;

            //Image table now needs all the UIDs in order to be extractable
            var adder = new TagColumnAdder("StudyInstanceUID", "varchar(100)", ImageTableInfo, new AcceptAllCheckNotifier());
            adder.Execute();
        }

        private ImageTableTemplateCollection GetDefaultTemplate(FAnsi.DatabaseType databaseType)
        {
            var collection = ImageTableTemplateCollection.LoadFrom(DefaultTemplateYaml);
            collection.DatabaseType = databaseType;
            return collection;
        }

        public void TruncateTablesIfExists()
        {
            foreach (var t in new[] { ImageTable, SeriesTable, StudyTable })
                if (t != null && t.Exists())
                    t.Truncate();
        }

        public DicomFileMessage GetDicomFileMessage(string fileSystemRoot, FileInfo fi)
        {
            var toReturn = new DicomFileMessage(fileSystemRoot, fi)
            {
                StudyInstanceUID = "999",
                SeriesInstanceUID = "999",
                SOPInstanceUID = "999",
                DicomFileSize = fi.Length
            };

            var ds = DicomFile.Open(fi.FullName).Dataset;
            ds.Remove(DicomTag.PixelData);

            toReturn.DicomDataset = DicomTypeTranslater.SerializeDatasetToJson(ds);

            return toReturn;
        }
        public DicomFileMessage GetDicomFileMessage(DicomDataset ds, string fileSystemRoot, string file)
        {
            var toReturn = new DicomFileMessage(fileSystemRoot, file)
            {
                StudyInstanceUID = "999",
                SeriesInstanceUID = "999",
                SOPInstanceUID = "999"
            };

            ds.Remove(DicomTag.PixelData);

            toReturn.DicomDataset = DicomTypeTranslater.SerializeDatasetToJson(ds);

            return toReturn;
        }

        const string DefaultTemplateYaml =
            @"Tables:
- TableName: StudyTable
  Columns:
  - ColumnName: PatientID
  - ColumnName: StudyInstanceUID
    IsPrimaryKey: true
  - ColumnName: StudyDate
    AllowNulls: true
  - ColumnName: StudyTime
    AllowNulls: true
  - ColumnName: ModalitiesInStudy
    AllowNulls: true
  - ColumnName: StudyDescription
    AllowNulls: true
  - ColumnName: AccessionNumber
    AllowNulls: true
  - ColumnName: PatientSex
    AllowNulls: true
  - ColumnName: PatientAge
    AllowNulls: true
  - ColumnName: NumberOfStudyRelatedInstances
    AllowNulls: true
- TableName: SeriesTable
  Columns:
  - ColumnName: StudyInstanceUID
  - ColumnName: SeriesInstanceUID
    IsPrimaryKey: true
  - ColumnName: Modality
    AllowNulls: true
  - ColumnName: SourceApplicationEntityTitle
    AllowNulls: true
  - ColumnName: InstitutionName
    AllowNulls: true
  - ColumnName: ProcedureCodeSequence
    AllowNulls: true
  - ColumnName: ProtocolName
    AllowNulls: true
  - ColumnName: PerformedProcedureStepID
    AllowNulls: true
  - ColumnName: PerformedProcedureStepDescription
    AllowNulls: true
  - ColumnName: SeriesDescription
    AllowNulls: true
  - ColumnName: BodyPartExamined
    AllowNulls: true
  - ColumnName: DeviceSerialNumber
    AllowNulls: true
  - ColumnName: NumberOfSeriesRelatedInstances
    AllowNulls: true
  - ColumnName: SeriesNumber
    AllowNulls: true
- TableName: ImageTable
  Columns:
  - ColumnName: SeriesInstanceUID
  - ColumnName: SOPInstanceUID
    IsPrimaryKey: true
  - ColumnName: SeriesDate
    AllowNulls: true
  - ColumnName: SeriesTime
    AllowNulls: true
  - ColumnName: BurnedInAnnotation
    AllowNulls: true
  - ColumnName: RelativeFileArchiveURI
    AllowNulls: true
  - ColumnName: MessageGuid
    AllowNulls: true
  - ColumnName: SliceLocation
    AllowNulls: true
  - ColumnName: SliceThickness
    AllowNulls: true
  - ColumnName: SpacingBetweenSlices
    AllowNulls: true
  - ColumnName: SpiralPitchFactor
    AllowNulls: true
  - ColumnName: KVP
    AllowNulls: true
  - ColumnName: ExposureTime
    AllowNulls: true
  - ColumnName: Exposure
    AllowNulls: true
  - ColumnName: ImageType
    AllowNulls: true
  - ColumnName: ManufacturerModelName
    AllowNulls: true
  - ColumnName: Manufacturer
    AllowNulls: true
  - ColumnName: XRayTubeCurrent
    AllowNulls: true
  - ColumnName: PhotometricInterpretation
    AllowNulls: true
  - ColumnName: ContrastBolusRoute
    AllowNulls: true
  - ColumnName: ContrastBolusAgent
    AllowNulls: true
  - ColumnName: AcquisitionNumber
    AllowNulls: true
  - ColumnName: AcquisitionDate
    AllowNulls: true
  - ColumnName: AcquisitionTime
    AllowNulls: true
  - ColumnName: ImagePositionPatient
    AllowNulls: true
  - ColumnName: PixelSpacing
    AllowNulls: true
  - ColumnName: FieldOfViewDimensions
    AllowNulls: true
  - ColumnName: FieldOfViewDimensionsInFloat
    AllowNulls: true
  - ColumnName: DerivationDescription
    AllowNulls: true
  - ColumnName: TransferSyntaxUID
    AllowNulls: true
  - ColumnName: LossyImageCompression
    AllowNulls: true
  - ColumnName: LossyImageCompressionMethod
    AllowNulls: true
  - ColumnName: LossyImageCompressionRatio
    AllowNulls: true
  - ColumnName: LossyImageCompressionRetired
    AllowNulls: true
  - ColumnName: ScanOptions
    AllowNulls: true
  - ColumnName: DicomFileSize
    AllowNulls: true
    Type:
      CSharpType: System.Int64
";
    }
}
