using System.Collections.Concurrent;
using System.Diagnostics;
using DicomTypeTranslation;
using FellowOakDicom;
using MongoDB.Bson;
using MongoDB.Driver;
using Smi.Common.Messages;
using Smi.Common.MongoDB;

namespace Applications.DicomLoader;

public static class Loader
{
    private static readonly object _flushLock=new ();
    private static int _fileCount;
    private static readonly ConcurrentDictionary<string,SeriesMessage> _seriesList=new ();
    private static readonly Stopwatch _timer = Stopwatch.StartNew();

    private static SeriesMessage LoadSm(string id, FileInfo fi, DicomDataset ds, string studyId)
    {
        // Try loading from Mongo in case we were interrupted previously
        var b=SeriesStore.Find(new BsonDocument("SeriesInstanceUID", id)).FirstOrDefault();
        return b ?? new SeriesMessage
        {
            DirectoryPath = fi.DirectoryName,
            DicomDataset = DicomTypeTranslater.SerializeDatasetToJson(ds),
            ImagesInSeries = 1,
            SeriesInstanceUID = id,
            StudyInstanceUID = studyId
        };
    }
    private static SeriesMessage IncSm(string _,SeriesMessage sm)
    {
        sm.ImagesInSeries++;
        return sm;
    }
    /// <summary>
    /// Write the pending Series data out to Mongo
    /// </summary>
    public static void Flush(bool force=false)
    {
        lock (_flushLock)
        {
            if (!force && _seriesList.Count < 100)
                return;
            SeriesStore?.InsertMany(_seriesList.Values);
            _seriesList.Clear();
        }
    }

    public static void Report()
    {
        if (_timer.ElapsedMilliseconds == 0) return;
        Console.WriteLine($"Processed {_fileCount} files in {_timer.ElapsedMilliseconds}ms ({1000*_fileCount/_timer.ElapsedMilliseconds} per second)");
    }
    private static void Process(FileInfo fi, IMongoCollection<BsonDocument> iStore, CancellationToken ct)
    {
        // Consider flushing every 256 file loads
        if ((Interlocked.Increment(ref _fileCount) & 0xff) == 0)
        {
            Flush();
            Report();
        }
        var ds = DicomFile.Open(fi.FullName).Dataset;
        var identifiers = new string[3];

        // Pre-fetch these to ensure they exist before we go further
        identifiers[0] = ds.GetValue<string>(DicomTag.StudyInstanceUID, 0);
        identifiers[1] = ds.GetValue<string>(DicomTag.SeriesInstanceUID, 0);
        identifiers[2] = ds.GetValue<string>(DicomTag.SOPInstanceUID, 0);

        if (identifiers.Any(string.IsNullOrWhiteSpace))
        {
            Console.WriteLine($"'{fi.FullName}' had blank DICOM UID");
            return;
        }

        DicomFileMessage message = new()
        {
            StudyInstanceUID = identifiers[0],
            SeriesInstanceUID = identifiers[1],
            SOPInstanceUID = identifiers[2],

            DicomFileSize = fi.Length,
            DicomFilePath = fi.FullName
        };
        // ReSharper disable once InconsistentlySynchronizedField
        _seriesList.AddOrUpdate(identifiers[1],id=>LoadSm(id,fi,ds,identifiers[0]) , IncSm);
        DicomDataset filtered = new(ds.Where(i => i is not DicomOtherByteFragment).ToArray());

        iStore.InsertOne(
            new BsonDocument("header", MongoDocumentHeaders.ImageDocumentHeader(message, new MessageHeader())).AddRange(
                DicomTypeTranslaterReader.BuildBsonDocument(filtered)), cancellationToken: ct);
    }
    public static ValueTask Load(string filename, CancellationToken ct)
    {
        if (ImageStore is null) throw new ArgumentNullException(nameof(ImageStore));
        if (SeriesStore is null) throw new ArgumentNullException(nameof(SeriesStore));
        if (Database is null) throw new ArgumentNullException(nameof(Database));

        if (!File.Exists(filename))
        {
            Console.WriteLine($@"{filename} does not exist, skipping");
            return ValueTask.CompletedTask;
        }

        if (ImageStore.CountDocuments(
                new BsonDocumentFilterDefinition<BsonDocument>(new BsonDocument("header", new BsonDocument("DicomFilePath", filename)))) > 0)
        {
            Console.WriteLine($@"{filename} already loaded, skipping");
            return ValueTask.CompletedTask;
        }
        Process(new FileInfo(filename),ImageStore, ct);
        return ValueTask.CompletedTask;
    }

    public static IMongoCollection<BsonDocument>? ImageStore { get; set; }
    public static IMongoCollection<SeriesMessage>? SeriesStore { get; set; }
    public static IMongoDatabase? Database { get; set; }
}