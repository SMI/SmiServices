#nullable enable
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DicomTypeTranslation;
using FellowOakDicom;
using MongoDB.Bson;
using MongoDB.Driver;
using SharpCompress.Readers;
using Smi.Common.Messages;
using Smi.Common.MongoDB;

namespace Applications.DicomLoader;

public static class Loader
{
    private static readonly object _flushLock=new ();
    private static int _fileCount;
    private static readonly ConcurrentDictionary<string,SeriesMessage> _seriesList=new ();
    private static readonly Stopwatch _timer = Stopwatch.StartNew();

    private static SeriesMessage LoadSm(string id, string directoryName, DicomDataset ds, string studyId)
    {
        // Try loading from Mongo in case we were interrupted previously
        var b=SeriesStore.Find(new BsonDocument("SeriesInstanceUID", id)).FirstOrDefault();
        return b ?? new SeriesMessage
        {
            DirectoryPath = directoryName,
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
            if (!_seriesList.IsEmpty)
                SeriesStore?.InsertMany(_seriesList.Values);
            _seriesList.Clear();
        }
    }

    public static void Report()
    {
        if (_timer.ElapsedMilliseconds == 0) return;
        Console.WriteLine($"Processed {_fileCount} files in {_timer.ElapsedMilliseconds}ms ({1000*_fileCount/_timer.ElapsedMilliseconds} per second)");
    }

    private static readonly byte[] _dicomMagic = Encoding.ASCII.GetBytes("DICM");

    private static void Process(FileInfo fi, IMongoCollection<BsonDocument> iStore, CancellationToken ct)
    {
        var bBuffer=new byte[132];
        var buffer = new Span<byte>(bBuffer);
        using var fileStream = File.OpenRead(fi.FullName);
        if (fileStream.Read(buffer) == 132 && buffer[128..].SequenceEqual(_dicomMagic))
        {
            var ds = DicomFile.Open(fileStream).Dataset;
            Process(ds,fi.FullName,fi.DirectoryName ?? throw new ApplicationException($"Unable to find parent directory for {fi.FullName}"),fi.Length, iStore, ct);
            return;
        }
        // Not DICOM? OK, try it as an archive:
        using var archiveStream = File.OpenRead(fi.FullName);
        using var archive=ReaderFactory.Open(archiveStream);
        while(archive.MoveToNextEntry())
        {
            if (archive.Entry.IsDirectory) continue;
            using var entry = archive.OpenEntryStream();
            var ds = DicomFile.Open(entry).Dataset;
            Process(ds,$"{fi.FullName}!{archive.Entry.Key}",fi.DirectoryName??throw new ApplicationException($"No parent directory for {fi.FullName}"),archive.Entry.Size,iStore,ct);
        }
    }

    private static void Process(DicomDataset ds, string path, string directoryName, long size,
        IMongoCollection<BsonDocument> iStore, CancellationToken ct)
    {
        // Consider flushing every 256 file loads
        if ((Interlocked.Increment(ref _fileCount) & 0xff) == 0)
        {
            Flush();
            Report();
        }

        var identifiers = new string[3];

        // Pre-fetch these to ensure they exist before we go further
        identifiers[0] = ds.GetValue<string>(DicomTag.StudyInstanceUID, 0);
        identifiers[1] = ds.GetValue<string>(DicomTag.SeriesInstanceUID, 0);
        identifiers[2] = ds.GetValue<string>(DicomTag.SOPInstanceUID, 0);

        if (identifiers.Any(string.IsNullOrWhiteSpace))
        {
            Console.WriteLine($"'{path}' had blank DICOM UID");
            return;
        }

        DicomFileMessage message = new()
        {
            StudyInstanceUID = identifiers[0],
            SeriesInstanceUID = identifiers[1],
            SOPInstanceUID = identifiers[2],

            DicomFileSize = size,
            DicomFilePath = path
        };
        // ReSharper disable once InconsistentlySynchronizedField
        _seriesList.AddOrUpdate(identifiers[1],id=>LoadSm(id,directoryName,ds,identifiers[0]) , IncSm);
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
                new BsonDocumentFilterDefinition<BsonDocument>(new BsonDocument("header", new BsonDocument("DicomFilePath", filename))),new CountOptions(),ct) > 0)
        {
            Console.WriteLine($@"{filename} already loaded, skipping");
            return ValueTask.CompletedTask;
        }

        try
        {
            Process(new FileInfo(filename), ImageStore, ct);
        }
        catch (Exception e)
        {
            Console.WriteLine($"{filename} processing failed to due {e}");
        }
        return ValueTask.CompletedTask;
    }

    public static IMongoCollection<BsonDocument>? ImageStore { get; set; }
    public static IMongoCollection<SeriesMessage>? SeriesStore { get; set; }
    public static IMongoDatabase? Database { get; set; }
}