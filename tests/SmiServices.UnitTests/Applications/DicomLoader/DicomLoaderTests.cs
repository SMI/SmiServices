using BadMedicine.Dicom;
using FAnsi;
using FellowOakDicom;
using MongoDB.Bson;
using NUnit.Framework;
using Rdmp.Core.Repositories;
using Rdmp.Dicom.PipelineComponents.DicomSources;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using SharpCompress.Compressors.Deflate;
using SharpCompress.Writers.Zip;
using SmiServices.Applications.DicomLoader;
using SmiServices.Common.Messages;
using SmiServices.Common.MongoDB;
using SmiServices.Common.Options;
using SmiServices.UnitTests.Common;
using SmiServices.UnitTests.Microservices.DicomRelationalMapper;
using SynthEHR;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Tests.Common;

namespace SmiServices.UnitTests.Applications.DicomLoader;

[RequiresRelationalDb(DatabaseType.MicrosoftSQLServer)]
public class DicomLoaderTests : DatabaseTests
{
    private GlobalOptions _gOptions = null!;

    [OneTimeSetUp]
    public void Setup()
    {
        _gOptions = new GlobalOptionsFactory().Load(nameof(DicomLoader));
        Debug.Assert(_gOptions.RDMPOptions != null, "_gOptions.RDMPOptions != null");
        var conn = (RepositoryLocator.CatalogueRepository as CatalogueRepository)?.ConnectionString;
        _gOptions.RDMPOptions.CatalogueConnectionString = conn;
        _gOptions.RDMPOptions.DataExportConnectionString = conn;
        var helper = new DicomRelationalMapperTestHelper();
        var db = GetCleanedServer(DatabaseType.MicrosoftSQLServer);
        helper.SetupSuite(db, RepositoryLocator, _gOptions, typeof(DicomDatasetCollectionSource));
        Debug.Assert(helper.LoadMetadata != null, "helper.LoadMetadata != null");
        _gOptions.DicomRelationalMapperOptions!.LoadMetadataId = helper.LoadMetadata?.ID ?? throw new Exception("No LoadMetadataId");
    }

    [Test]
    [RequiresExternalService]
    public void BatchLoadTest()
    {
        DicomDataset[] testImages;
        Study study;

        var database = MongoClientHelpers.GetMongoClient(_gOptions.MongoDatabases!.DicomStoreOptions!, nameof(DicomLoader)).GetDatabase(_gOptions.MongoDatabases.DicomStoreOptions!.DatabaseName);
        var imageStore = database.GetCollection<BsonDocument>(_gOptions.MongoDbPopulatorOptions!.ImageCollection);
        var seriesStore = database.GetCollection<SeriesMessage>(_gOptions.MongoDbPopulatorOptions.SeriesCollection);

        imageStore.DeleteMany(new BsonDocument());
        seriesStore.DeleteMany(new BsonDocument());
        Assert.Multiple(() =>
        {
            Assert.That(imageStore.CountDocuments(new BsonDocument()), Is.EqualTo(0));
            Assert.That(seriesStore.CountDocuments(new BsonDocument()), Is.EqualTo(0));
        });

        // Create a bunch of (pixel-free) DICOM files
        Random r = new(321);
        var di = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, nameof(DicomLoader)));
        if (di.Exists)
            di.Delete(true);
        using (var generator = new DicomDataGenerator(r, di.FullName, "CT") { NoPixels = true })
        {
            generator.GenerateTestDataRow(new Person(r));
            testImages = generator.GenerateStudyImages(new Person(r), out study);
        }

        // Move 10 of the DICOM files into a ZIP archive to test that function too
        var archiveName = Path.Combine(di.FullName, "arctest.zip");
        var zipFiles = di.GetFiles("*.dcm", new EnumerationOptions() { RecurseSubdirectories = true })[..10];
        using (var archiveStream = File.OpenWrite(archiveName))
        using (var archiver = ZipArchive.Create())
        {
            foreach (var entry in zipFiles)
                archiver.AddEntry(entry.Name, entry.FullName);
            archiver.SaveTo(archiveStream, new ZipWriterOptions(CompressionType.Deflate) { LeaveStreamOpen = true, DeflateCompressionLevel = CompressionLevel.BestSpeed });
        }
        // Need to delete source files _after_ the archive writer is .Disposed
        foreach (var entry in zipFiles)
            entry.Delete();

        // Make a list of the files we have, and move some into a 7z file for testing
        var fileNames = di.GetFiles("*", new EnumerationOptions { RecurseSubdirectories = true }).Select(x => x.FullName);
        var files = string.Join('\0', fileNames);
        using var fileList = new MemoryStream(Encoding.UTF8.GetBytes(files));
        typeof(Program).GetMethod("OnParse", BindingFlags.NonPublic | BindingFlags.Static,
                new[] { typeof(GlobalOptions), typeof(DicomLoaderOptions), typeof(Stream) })!
            .Invoke(null, new object[] { _gOptions, new DicomLoaderOptions() { LoadSql = true }, fileList });
        Assert.Multiple(() =>
        {
            //Program.OnParse(_gOptions,_dOptions,fileList);

            Assert.That(imageStore.CountDocuments(new BsonDocument()), Is.EqualTo(testImages.Length));
            Assert.That(seriesStore.CountDocuments(new BsonDocument()), Is.EqualTo(study.Series.Count));
        });
    }
}
