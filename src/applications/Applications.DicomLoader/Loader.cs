using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
    private readonly Stopwatch _timer;
    private readonly IMongoCollection<BsonDocument> _imageStore;
    private readonly IMongoCollection<SeriesMessage> _seriesStore;
    private List<ValueTuple<DicomFileMessage,DicomDataset>> _imageQueue;
    private static readonly RecyclableMemoryStreamManager _streamManager;
    private volatile bool _backlogged, _flushing;
    private readonly object _statsLock,_imageQueueLock;

    // Yes, we lock the series list. Read-only while accessing, write while flushing.
    private readonly ReaderWriterLockSlim _seriesListLock;
    private ConcurrentDictionary<string, SeriesMessage> _seriesList;

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

    private static SeriesMessage IncSm(string _,SeriesMessage sm)
    {
        lock(sm)
            sm.ImagesInSeries++;
        return sm;
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
        try
        {
            if (_parallelDleHost != null && _lmd != null) FlushRelational(_parallelDleHost,_lmd, imageBatch);
        }
        finally
        {
            mf.Wait();
            _backlogged = false;
        }
    }

    public static void FlushRelational(ParallelDLEHost host,LoadMetadata lmd,IEnumerable<(DicomFileMessage,DicomDataset)> imageBatch)
    {
        int count;
        var lockTimer = Stopwatch.StartNew();
        long lockWait;
        lock (host)
        {
            lockWait = lockTimer.ElapsedMilliseconds;
            var workList = new DicomFileMessageToDatasetListWorklist(imageBatch.Select(i =>
                new QueuedImage(new MessageHeader(), 0, i.Item1, i.Item2)));
            try
            {
                var result = host.RunDLE(lmd, workList);
                if (result is not ExitCodeType.Success and not ExitCodeType.OperationNotRequired)
                    Console.Error.WriteLine($"DLE load failed with result {result}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"***** ABEND *****\nRunDLE blew up with: {e}");
                throw;
            }

            count = workList.Count;
        }

        Console.WriteLine(
            $"SQL load completed on {count} items in {lockTimer.ElapsedMilliseconds}ms, {lockWait}ms lock contention");
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

    private static readonly InsertManyOptions _insertManyOptions = new () { IsOrdered = false };
    private async Task MongoImageFlush(ArraySegment<(DicomFileMessage, DicomDataset)> imageBatch)
    {
        // Delete pre-existing entries, if applicable, then insert our queue:
        if (_loadOptions.DeleteConflicts)
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
                        .AddRange(DicomTypeTranslaterReader.BuildBsonDocument(i.Item2))), _insertManyOptions);
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

        // Grab the seriesList _write_ lock, we're about to swap it for a new one
        ConcurrentDictionary<string, SeriesMessage> tmp;
        long lockStart;
        long lockTime;
        int waiting;
        try
        {
            lockStart = mongoStopwatch.ElapsedMilliseconds;
            _seriesListLock.EnterWriteLock();
            lockTime = mongoStopwatch.ElapsedMilliseconds;
            tmp = _seriesList;
            _seriesList = new ConcurrentDictionary<string, SeriesMessage>(Environment.ProcessorCount,_seriesList.TryGetNonEnumeratedCount(out var size)?size:1000);
            waiting = _seriesListLock.WaitingReadCount;
        }
        finally
        {
            _seriesListLock.ExitWriteLock();
        }
        var seriesBatch = tmp.Values.ToArray();
        var seriesCount = seriesBatch.Length;
        var chunk = new ArraySegment<string>(seriesBatch.Select(s => s.SeriesInstanceUID).ToArray());
        while (chunk.Count > 1000)
        {
            await _seriesStore.DeleteManyAsync(
                Builders<SeriesMessage>.Filter.In("SeriesInstanceUID", chunk[..1000]));
            chunk = chunk[1000..];
        }
        await _seriesStore.DeleteManyAsync(Builders<SeriesMessage>.Filter.In("SeriesInstanceUID",
            chunk));
        var deleteTime = mongoStopwatch.ElapsedMilliseconds;
        try
        {
            await _seriesStore.InsertManyAsync(seriesBatch, _insertManyOptions);
        }
        catch (Exception e)
        {
            Console.WriteLine($"MongoDB series write:{e.Message}");
        }


        await Console.Error.WriteLineAsync(
            $"Flushed {seriesCount} Series objects to Mongo in {mongoStopwatch.ElapsedMilliseconds}ms. Waited {lockTime-lockStart}ms for write lock, had {waiting} threads waiting after swap, took {deleteTime-lockTime}ms to delete and {mongoStopwatch.ElapsedMilliseconds-deleteTime}ms to write.");
    }

    public void Report(CancellationToken? cancellationToken=null)
    {
        if (_timer.ElapsedMilliseconds == 0) return;
        cancellationToken?.ThrowIfCancellationRequested();
        var memoryLimit = _loadOptions.RamLimit * 1024 * 1024 * 1024;
        bool shouldFlush;
        long used;
        lock (_statsLock)
        {
            used = GC.GetTotalMemory(false);
            _backlogged = used > memoryLimit * 3;
            shouldFlush = !_flushing && used > memoryLimit;
            if (shouldFlush)
            {
                _flushing = true;
            }
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
        _seriesListLock = new ReaderWriterLockSlim();
        _loadOptions = loadOptions;
        lock(_imageQueueLock)
            _imageQueue = new List<(DicomFileMessage, DicomDataset)>();

        try
        {
            _seriesListLock.EnterWriteLock();
            _seriesList = new ConcurrentDictionary<string, SeriesMessage>(Environment.ProcessorCount, 1000);
        }
        finally
        {
            _seriesListLock.ExitWriteLock();
        }
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
        ct.ThrowIfCancellationRequested();
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
                ds = new DicomDataset(ds.Where(i => i is not DicomOtherByteFragment).ToArray());
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
                ct.ThrowIfCancellationRequested();
                try
                {
                    var path = $"{fi.FullName}!{entry.Name}";
                    if (!_loadOptions.DeleteConflicts && ExistingEntry(path))
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
                    ds = new DicomDataset(ds.Where(i => i is not DicomOtherByteFragment).ToArray());
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
        while (_backlogged && !ct.IsCancellationRequested)
            ct.WaitHandle.WaitOne(10_000);
        ct.ThrowIfCancellationRequested();

        // Update stats and consider flushing every 256 file loads
        if ((Interlocked.Increment(ref _dicomCount) & 0xff) == 0) Report(ct);
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
        var series = identifiers[1];
        try
        {
            _seriesListLock.EnterReadLock();
            _seriesList.AddOrUpdate(series, _ => LoadSm(series, directoryName, ds, identifiers[0]), IncSm);
        }
        finally
        {
            _seriesListLock.ExitReadLock();
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

        if (!_loadOptions.DeleteConflicts && ExistingEntry(filename))
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

    public static (DicomFileMessage,DicomDataset) ParseBson(BsonDocument arg)
    {
        // TODO: Recover file header and dataset from Bson
        var ds = DicomTypeTranslaterWriter.BuildDicomDataset(arg);
        var header=arg.GetElement("header").Value as BsonDocument ?? throw new Exception($"No header in {arg}");
        var msg = new DicomFileMessage
        {
            StudyInstanceUID = arg.GetElement("StudyInstanceUID").Value.AsString,
            DicomFilePath = header.GetElement("DicomFilePath").Value.AsString,
            DicomFileSize = header.GetElement("DicomFileSize").Value.AsInt64,
            SOPInstanceUID = arg.GetElement("SOPInstanceUID").Value.AsString,
            SeriesInstanceUID = arg.GetElement("SeriesInstanceUID").Value.AsString
        };
        return (msg, ds);
    }
}
