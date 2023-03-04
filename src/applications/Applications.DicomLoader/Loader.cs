using System;
using System.Collections.Generic;
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
using Microservices.DicomRelationalMapper.Execution;
using Microservices.DicomRelationalMapper.Messaging;
using Microsoft.IO;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Rdmp.Core.Curation.Data.DataLoad;
using Rdmp.Core.DataLoad;
using Smi.Common.Messages;
using Smi.Common.MongoDB;

namespace Applications.DicomLoader;

public class Loader
{
    private long _dicomCount,_fileCount;
    private readonly Dictionary<string,SeriesMessage> _seriesList;
    private readonly Stopwatch _timer;
    private readonly IMongoCollection<BsonDocument> _imageStore;
    private readonly IMongoCollection<SeriesMessage> _seriesStore;
    private List<ValueTuple<DicomFileMessage,DicomDataset>> _imageQueue;
    private static readonly RecyclableMemoryStreamManager _streamManager;
    private volatile bool _backlogged, _flushing;
    private readonly object _statsLock,_imageQueueLock,_seriesListLock;

    /// <summary>
    /// Make sure Mongo ignores its internal-only _id attribute when
    /// re-loading saved SeriesMessage instances. ALso disable fo-dicom
    /// validation: we'd rather copy data accurately than enforce DICOM
    /// compliance at this level.
    /// </summary>
    static Loader()
    {
        _streamManager = new RecyclableMemoryStreamManager();
        if (BsonClassMap.GetRegisteredClassMaps().All(m => m.ClassType != typeof(SeriesMessage)))
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
            DicomDataset = DicomTypeTranslater.SerializeDatasetToJson(new DicomDataset(ds.Where(i => i is not DicomOtherByteFragment).ToArray())),
            ImagesInSeries = 1,
            SeriesInstanceUID = id,
            StudyInstanceUID = studyId
        };
    }

    /// <summary>
    /// Write the pending data out to Mongo and optionally SQL
    /// </summary>
    public void Flush()
    {
        (DicomFileMessage, DicomDataset)[] imageBatch;
        lock (_imageQueueLock)
        {
            imageBatch = _imageQueue.ToArray();
            _imageQueue = new List<(DicomFileMessage, DicomDataset)>(imageBatch.Length);
        }

        using var mf=MongoFlush(new ArraySegment<(DicomFileMessage, DicomDataset)>(imageBatch));

        if (_parallelDleHost != null)
        {
            Stopwatch lockTimer = new();
            lockTimer.Start();
            long lockWait;
            lock (_parallelDleHost)
            {
                lockWait = lockTimer.ElapsedMilliseconds;
                var workList = new DicomFileMessageToDatasetListWorklist(imageBatch.Select(i =>
                    new QueuedImage(new MessageHeader(), 0, i.Item1, i.Item2)));
                try
                {
                    var result=_parallelDleHost.RunDLE(_lmd, workList);
                    if (result is not ExitCodeType.Success and not ExitCodeType.OperationNotRequired)
                        Console.Error.WriteLine($"DLE load failed with result {result}");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"***** ABEND *****\nRunDLE blew up with: {e}");
                    _backlogged = false;
                    throw;
                }
            }
            Console.WriteLine($"SQL load completed on {imageBatch.Length} items in {lockTimer.ElapsedMilliseconds}ms, {lockWait}ms lock contention");
        }

        mf.Wait();

        lock (_statsLock)
        {
            _backlogged = false;
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(2, GCCollectionMode.Forced, true, true);
        }
    }

    private async Task MongoFlush(ArraySegment<(DicomFileMessage, DicomDataset)> imageBatch)
    {
        await MongoSeriesFlush();
        while (imageBatch.Count > 10_000)
        {
            await MongoImageFlush(imageBatch[..10_000]);
            imageBatch = imageBatch[10_000..];
        }
        await MongoImageFlush(imageBatch);
    }

    private async Task MongoImageFlush(ArraySegment<(DicomFileMessage, DicomDataset)> imageBatch)
    {
        // Delete pre-existing entries, if applicable, then insert our queue:
        if (_loadOptions.ForceReload)
        {
            var sw = Stopwatch.StartNew();
            var builder = Builders<BsonDocument>.Filter;
            await _imageStore.DeleteManyAsync(builder.In("header.DicomFilePath",
                imageBatch.Select(i => i.Item1.DicomFilePath)));
            await _imageStore.DeleteManyAsync(builder.In("header.SOPInstanceUID",
                imageBatch.Select(i => i.Item1.SOPInstanceUID)));
            await Console.Error.WriteLineAsync(
                $"Deleted {imageBatch.Count} entries from Mongo in {sw.ElapsedMilliseconds}ms");
        }

        var mongoStopwatch = Stopwatch.StartNew();
        try
        {
            await _imageStore.InsertManyAsync(imageBatch
                .AsParallel()
                .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                .Select(i =>
                    new BsonDocument("header", MongoDocumentHeaders.ImageDocumentHeader(i.Item1, new MessageHeader()))
                        .AddRange(DicomTypeTranslaterReader.BuildBsonDocument(i.Item2))));
        }
        catch (Exception e)
        {
            await Console.Error.WriteLineAsync($"Image flush:{e.Message}");
        }
        await Console.Error.WriteLineAsync(
            $"Inserted {imageBatch.Count} entries to Mongo in {mongoStopwatch.ElapsedMilliseconds}ms");
    }

    // Now flush the SeriesMessage list to Mongo:
    private async Task MongoSeriesFlush()
    {
        var mongoStopwatch = Stopwatch.StartNew();
        int seriesCount;
        lock (_seriesListLock)
        {
            seriesCount = _seriesList.Count;
            _seriesStore.DeleteMany(Builders<SeriesMessage>.Filter.In("SeriesInstanceUID",
                _seriesList.Values.Select(s => s.SeriesInstanceUID)));
            try
            {
                _seriesStore.InsertMany(_seriesList.Values);
            }
            catch (Exception e)
            {
                Console.WriteLine($"MongoDB:{e.Message}");
            }

            _seriesList.Clear();
        }

        await Console.Error.WriteLineAsync(
            $"Stored {seriesCount} Series objects to Mongo in {mongoStopwatch.ElapsedMilliseconds}ms");
    }

    public void Report()
    {
        if (_timer.ElapsedMilliseconds == 0) return;
        var memoryLimit = _loadOptions.MemoryLimit * 1024 * 1024 * 1024;
        bool shouldFlush;
        long used;
        lock (_statsLock)
        {
            used = GC.GetTotalMemory(false);
            if (used <= memoryLimit*3)
            {
                Console.WriteLine($"Processed {_dicomCount} DICOM objects from {_fileCount} files in {_timer.ElapsedMilliseconds / 1000}s ({1000 * _dicomCount / _timer.ElapsedMilliseconds} per second) using {used / (1024 * 1024)}MiB");
                return;
            }

            var sw = Stopwatch.StartNew();
            var newUsed = GC.GetTotalMemory(true);
            Console.Error.WriteLine(
                $"GC trimmed memory footprint from {used / (1 << 20)}MiB to {newUsed / (1 << 20)}MiB in {sw.ElapsedMilliseconds}ms");
            used = newUsed;

            shouldFlush = !_flushing && used > memoryLimit;
            if (shouldFlush)
            {
                _flushing = true;
            }
            _backlogged = used > _loadOptions.MemoryLimit * 1024 * (1024 * 1024)*2;
        }

        Console.WriteLine($"Processed {_dicomCount} DICOM objects from {_fileCount} files in {_timer.ElapsedMilliseconds/1000}s ({1000 * _dicomCount / _timer.ElapsedMilliseconds} per second) using {used/(1024*1024)}MiB{(shouldFlush?" and about to flush":"")}");
        if (!shouldFlush) return;
        try
        {
            Flush();
        }
        finally
        {
            _flushing = false;
        }
    }

    private static readonly byte[] _dicomMagic = Encoding.UTF8.GetBytes("DICM"); // Can be "DICM"u8 once we reach C# 11.0!
    private readonly DicomLoaderOptions _loadOptions;
    private readonly ParallelDLEHost? _parallelDleHost;
    private readonly LoadMetadata? _lmd;

    public Loader(IMongoDatabase database, string imageCollection, string seriesCollection,
        DicomLoaderOptions loadOptions, ParallelDLEHost? parallelDleHost,LoadMetadata? lmd)
    {
        _imageQueueLock = new object();
        _seriesListLock = new object();
        _loadOptions = loadOptions;
        lock(_imageQueueLock)
            _imageQueue = new List<(DicomFileMessage, DicomDataset)>();
        lock(_seriesListLock)
            _seriesList = new Dictionary<string, SeriesMessage>();
        _timer = Stopwatch.StartNew();
        _parallelDleHost = parallelDleHost;
        _lmd = lmd;
        _statsLock = new object();
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

                // Strip pixel data ASAP: we don't want it anyway
                ds.Remove(DicomTag.PixelData);
                Interlocked.Increment(ref _fileCount);
                Process(ds, fi.FullName,dName, fi.Length, ct);
                return;
            }
        }
        // Not DICOM? OK, try it as an archive:
        try
        {
            using var archive = new LibArchiveReader(fi.FullName,64* (1 << 20));
            Interlocked.Increment(ref _fileCount);
            foreach (var entry in archive.Entries())
            {
                if (ct.IsCancellationRequested)
                    return;
                try
                {
                    var path = $"{fi.FullName}!{entry.Name}";
                    if (!_loadOptions.ForceReload && ExistingEntry(path))
                    {
                        return; // Exit whole archive processing on duplicate, unless force-reloading
                    }

                    long fileSize;
                    DicomDataset ds;
                    using (var ms = _streamManager.GetStream())
                    {
                        using (var eStream = entry.Stream)
                            eStream.CopyTo(ms);
                        if (ms.Length <= 0)
                            continue;
                        ms.Seek(0, SeekOrigin.Begin);
                        ds = DicomFile.Open(ms, FileReadOption.ReadAll).Dataset;
                        fileSize = ms.Length;
                    }
                    // Strip pixel data ASAP: we don't want it anyway
                    ds.Remove(DicomTag.PixelData);
                    Process(ds, path, dName, fileSize, ct);
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
        // Throttle to avoid runaway backlog if doing SQL loads:
        while (_backlogged)
            Thread.Sleep(10_000);

        // Update stats and consider flushing every 256 file loads
        if ((Interlocked.Increment(ref _dicomCount) & 0xff) == 0) Report();
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
        ds = new DicomDataset(ds.Where(i => i is not DicomOtherByteFragment).ToArray());
        lock (_seriesListLock)
        {
            if (_seriesList.ContainsKey(identifiers[1]))
                _seriesList[identifiers[1]].ImagesInSeries++;
            else
            {
                _seriesList[identifiers[1]] = LoadSm(identifiers[1], directoryName, ds, identifiers[0]);
            }
        }
        lock(_imageQueueLock)
            _imageQueue.Add((message, ds));
    }

    /// <summary>
    /// Check if a filename is an existing MongoDB entry
    /// </summary>
    /// <param name="filename">Filename (possibly in the form archive!entry) to check for</param>
    /// <returns>Whether there's already an entry for this file</returns>
    private bool ExistingEntry(string filename)
    {
        return _imageStore.AsQueryable().Any(i => i["header.DicomFilePath"].CompareTo(filename) == 0);
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

        if (ExistingEntry(filename))
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
