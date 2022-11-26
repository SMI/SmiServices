using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DicomTypeTranslation;
using FellowOakDicom;
using LibArchive.Net;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Smi.Common.Messages;
using Smi.Common.MongoDB;

namespace Applications.DicomLoader;

public class Loader
{
    private readonly object _flushLock=new ();
    private int _fileCount;
    private readonly ConcurrentDictionary<string,SeriesMessage> _seriesList=new ();
    private readonly Stopwatch _timer = Stopwatch.StartNew();
    private readonly IMongoCollection<BsonDocument> _imageStore;
    private readonly IMongoCollection<SeriesMessage> _seriesStore;
    private static readonly InsertManyOptions mongoinsertoptions = new()
    {
        
    };

    /// <summary>
    /// Make sure Mongo ignores its internal-only _id attribute when
    /// re-loading saved SeriesMessage instances. ALso disable fo-dicom
    /// validation: we'd rather copy data accurately than enforce DICOM
    /// compliance at this level.
    /// </summary>
    static Loader()
    {
        BsonClassMap.RegisterClassMap<SeriesMessage>(map =>
        {
            map.AutoMap();
            map.SetIgnoreExtraElements(true);
        });
        new DicomSetupBuilder().SkipValidation();
    }
    
    private SeriesMessage LoadSm(string id, string directoryName, DicomDataset ds, string studyId)
    {
        // Try loading from Mongo in case we were interrupted previously
        var b=_seriesStore.Find(new BsonDocument("SeriesInstanceUID", id)).FirstOrDefault();
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
    public void Flush(bool force=false)
    {
        lock (_flushLock)
        {
            if (!force && _seriesList.Count < 100)
                return;
            if (!_seriesList.IsEmpty)
                try
                {
                    _seriesStore.InsertMany(_seriesList.Values);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"MongoDB:{e.Message}");
                }
            _seriesList.Clear();
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(2, GCCollectionMode.Forced, true, true);
        }
    }

    public void Report()
    {
        if (_timer.ElapsedMilliseconds == 0) return;
        Console.WriteLine($"Processed {_fileCount} files in {_timer.ElapsedMilliseconds}ms ({1000*_fileCount/_timer.ElapsedMilliseconds} per second)");
    }

    private static readonly byte[] _dicomMagic = Encoding.ASCII.GetBytes("DICM");

    public Loader(IMongoDatabase database, string imageCollection, string seriesCollection, bool forceReload)
    {
        _imageStore = database.GetCollection<BsonDocument>(imageCollection);
        _seriesStore = database.GetCollection<SeriesMessage>(seriesCollection);
    }

    /// <summary>
    /// Open a file and load it (if DICOM) or its contents (if an archive)
    /// </summary>
    /// <param name="fi">DICOM file or archive of DICOM files to load</param>
    /// <param name="ct">Cancellation token</param>
    /// <exception cref="ApplicationException"></exception>
    private void Process(FileInfo fi, CancellationToken ct)
    {
        var dName = fi.DirectoryName ?? throw new ApplicationException($"No parent directory for '{fi.FullName}'");
        var bBuffer=new byte[132];
        var buffer = new Span<byte>(bBuffer);
        using (var fileStream = File.OpenRead(fi.FullName))
        {
            if (fileStream.Read(buffer) == 132 && buffer[128..].SequenceEqual(_dicomMagic))
            {
                var ds = DicomFile.Open(fileStream).Dataset;
                Process(ds, fi.FullName,dName, fi.Length, ct);
                return;
            }
        }
        // Not DICOM? OK, try it as an archive:
        try
        {
            using var archive = new LibArchiveReader(fi.FullName);
            foreach(var entry in archive.Entries())
            {
                if (ct.IsCancellationRequested)
                    return;
                try
                {
                    var path = $"{fi.FullName}!{entry.Name}";
                    if (!ExistingEntry(path, ct))
                    {
                        using var ms = new MemoryStream();
                        using (var eStream = entry.Stream)
                            eStream.CopyTo(ms);
                        if (ms.Length <= 0)
                            continue;
                        ms.Seek(0, SeekOrigin.Begin);
                        var ds = DicomFile.Open(ms, FileReadOption.ReadAll).Dataset;
                        Process(ds, path, dName, ms.Length, ct);
                    }
                }
                catch (DicomFileException e)
                {
                    Console.WriteLine($"{fi.FullName}!{entry.Name}:{e.Message}");
                }
            }
        }
        catch (ApplicationException e)
        {
            Console.WriteLine($"Unable to read {fi.FullName} as DICOM or archive: {e.Message}");
        }
    }

    /// <summary>
    /// Do the actual work of loading the DICOM dataset which came from a 'file' (or archive entry)
    /// </summary>
    /// <param name="ds">Dataset to load</param>
    /// <param name="path">Filename or archive entry (/data/foo.zip!file.dcm) from which ds came</param>
    /// <param name="directoryName">The directory name from which we're loading</param>
    /// <param name="size">File or archive entry size in bytes</param>
    /// <param name="ct">Cancellation token</param>
    private void Process(DicomDataset ds, string path, string directoryName, long size, CancellationToken ct)
    {
        // Consider flushing every 256 file loads
        if ((Interlocked.Increment(ref _fileCount) & 0xff) == 0)
        {
            Flush();
            Report();
        }
        if (ct.IsCancellationRequested)
            return;

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

        _imageStore.InsertOne(
            new BsonDocument("header", MongoDocumentHeaders.ImageDocumentHeader(message, new MessageHeader())).AddRange(
                DicomTypeTranslaterReader.BuildBsonDocument(filtered)), cancellationToken: ct);
    }

    /// <summary>
    /// Check if a filename is an existing MongoDB entry
    /// </summary>
    /// <param name="filename">Filename (possibly in the form archive!entry) to check for</param>
    /// <param name="ct"></param>
    /// <returns>Whether there's already an entry for this file</returns>
    private bool ExistingEntry(string filename, CancellationToken ct)
    {
        return (_imageStore.CountDocuments(
            new BsonDocumentFilterDefinition<BsonDocument>(new BsonDocument("header",
                new BsonDocument("DicomFilePath", filename))), new CountOptions(), ct) > 0);
    }

    /// <summary>
    /// Try to load the named DICOM file or archive of DICOM files into Mongo, ignoring if duplicate
    /// </summary>
    /// <param name="filename">File or archive to load</param>
    /// <param name="ct">Cancellation token for graceful cancellations</param>
    /// <returns></returns>
    public ValueTask Load(string filename, CancellationToken ct)
    {
        if (!File.Exists(filename))
        {
            Console.WriteLine($@"{filename} does not exist, skipping");
            return ValueTask.CompletedTask;
        }

        if (ExistingEntry(filename, ct))
        {
            return ValueTask.CompletedTask;
        }

        try
        {
            Process(new FileInfo(filename), ct);
        }
        catch (Exception e)
        {
            Console.WriteLine($"{filename}:{e}");
        }
        return ValueTask.CompletedTask;
    }
}