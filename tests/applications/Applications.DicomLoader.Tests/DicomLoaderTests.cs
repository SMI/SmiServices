using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BadMedicine;
using BadMedicine.Dicom;
using MongoDB.Bson;
using Smi.Common.Messages;
using Smi.Common.MongoDB;
using Smi.Common.Options;

namespace Applications.DicomLoader.Tests;

public class DicomLoaderTests
{
    private static GlobalOptions _gOptions = null!;

    [OneTimeSetUp]
    public void Setup()
    {
        _gOptions = new GlobalOptionsFactory().Load(nameof(DicomLoader));
    }

    [Test]
    public void Test1()
    {
        var database = MongoClientHelpers.GetMongoClient(_gOptions.MongoDatabases.DicomStoreOptions, nameof(DicomLoader)).GetDatabase(_gOptions.MongoDatabases.DicomStoreOptions.DatabaseName);
        var imageStore = database.GetCollection<BsonDocument>(_gOptions.MongoDbPopulatorOptions.ImageCollection);
        var seriesStore = database.GetCollection<SeriesMessage>(_gOptions.MongoDbPopulatorOptions.SeriesCollection);

        imageStore.DeleteMany(new BsonDocument());
        seriesStore.DeleteMany(new BsonDocument());
        Assert.That(imageStore.CountDocuments(new BsonDocument()), Is.EqualTo(0));
        Assert.That(seriesStore.CountDocuments(new BsonDocument()), Is.EqualTo(0));

        Random r = new(321);
        var di = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory,nameof(DicomLoader)));
        if (di.Exists)
            di.Delete(true);
        using var generator = new DicomDataGenerator(r, di.FullName, "CT") {NoPixels = true};
        generator.GenerateTestDataRow(new Person(r));
        var testImages=generator.GenerateStudyImages(new Person(r), out var study);
        var files = string.Join('\0',di.GetFiles("*",new EnumerationOptions {RecurseSubdirectories = true}).Select(x => x.FullName));
        using var fileList=new MemoryStream(Encoding.UTF8.GetBytes(files));
        typeof(Program).GetMethod("OnParse", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[]{_gOptions, null!, fileList});
        //Program.OnParse(_gOptions,null!,fileList);

        Assert.That(imageStore.CountDocuments(new BsonDocument()), Is.EqualTo(testImages.Length));
        Assert.That(seriesStore.CountDocuments(new BsonDocument()), Is.EqualTo(study.Series.Count));
    }
}