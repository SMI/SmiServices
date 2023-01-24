using System;
using System.Collections.Concurrent;
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
using Microsoft.IdentityModel.Tokens;
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
    private readonly object _flushLock=new ();
    private long _fileCount;
    private readonly ConcurrentDictionary<string,SeriesMessage> _seriesList=new ();
    private readonly Stopwatch _timer = Stopwatch.StartNew();
    private readonly IMongoCollection<BsonDocument> _imageStore;
    private readonly IMongoCollection<SeriesMessage> _seriesStore;
    private readonly ConcurrentQueue<ValueTuple<DicomFileMessage,DicomDataset>> _imageQueue=new();

    /// <summary>
    /// Make sure Mongo ignores its internal-only _id attribute when
    /// re-loading saved SeriesMessage instances. ALso disable fo-dicom
    /// validation: we'd rather copy data accurately than enforce DICOM
    /// compliance at this level.
    /// </summary>
    static Loader()
    {
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
        sm.ImagesInSeries++;
        return sm;
    }
    /// <summary>
    /// Write the pending Series data out to Mongo
    /// </summary>
    public void Flush(bool force=false)
    {
        if (!force && _imageQueue.Count < 1000)
            return;
        List<(DicomFileMessage, DicomDataset)> imageBatch;
        lock (_flushLock)
        {
            // Duplicate this check, because things could have changed while we waited for the lock:
            if (!force && _imageQueue.Count < 1000)
                return;
            imageBatch = new List<(DicomFileMessage, DicomDataset)>();
            while (_imageQueue.TryDequeue(out var I))
            {
                imageBatch.Add(I);
            }
        }

        // Nothing to do? Return early:
        if (!force && imageBatch.IsNullOrEmpty())
            return;

        // Delete pre-existing entries, if applicable, then insert our queue:
        if (_loadOptions.ForceReload)
        {
            var builder = Builders<BsonDocument>.Filter;
            foreach (var (dicomFileMessage, _) in imageBatch)
            {
                _imageStore.DeleteOne(builder.Eq("header.DicomFilePath", dicomFileMessage.DicomFilePath));
            }
        }
        try
        {
            _imageStore.InsertMany(imageBatch.Select(i=>
                new BsonDocument("header", MongoDocumentHeaders.ImageDocumentHeader(i.Item1, new MessageHeader()))
                    .AddRange(DicomTypeTranslaterReader.BuildBsonDocument(i.Item2))));
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Image flush:{e.Message}");
        }


        if (force || _seriesList.Count > 100)
        {
            // Now flush the SeriesMessage list to Mongo:
            try
            {
                List<SeriesMessage> sm = new();
                foreach (var key in _seriesList.Keys)
                {
                    if (_seriesList.Remove(key, out var seriesItem))
                    {
                        sm.Add(seriesItem);
                    }
                }
                _seriesStore.InsertMany(sm);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"MongoDB:{e.Message}");
            }
        }

        if (_parallelDleHost != null)
        {
            Stopwatch lockTimer = new();
            lockTimer.Start();
            long lockWait;
            lock (_parallelDleHost)
            {
                lockWait = lockTimer.ElapsedMilliseconds;
                var imageList = new List<QueuedImage>();
                imageBatch.Each(i =>
                {
                    imageList.Add(new QueuedImage(new MessageHeader(),0,i.Item1,i.Item2));
                });
                var workList = new DicomFileMessageToDatasetListWorklist(imageList);
                var result=_parallelDleHost.RunDLE(_lmd, workList);
                if (result!=ExitCodeType.Success && result!=ExitCodeType.OperationNotRequired)
                    Console.Error.WriteLine($"DLE load failed with result {result}");
            }
            Console.WriteLine($"SQL load completed on {imageBatch.Count} items in {lockTimer.ElapsedMilliseconds}ms, {lockWait}ms lock contention");
        }
        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
        GC.Collect(2, GCCollectionMode.Forced, true, true);
    }

    public void Report()
    {
        if (_timer.ElapsedMilliseconds == 0) return;
        Console.WriteLine($"Processed {_fileCount} files in {_timer.ElapsedMilliseconds}ms ({1000*_fileCount/_timer.ElapsedMilliseconds} per second)");
    }

    private static readonly byte[] _dicomMagic = Encoding.ASCII.GetBytes("DICM");
    private readonly DicomLoaderOptions _loadOptions;
    private readonly ParallelDLEHost? _parallelDleHost;
    private readonly LoadMetadata? _lmd;

    public Loader(IMongoDatabase database, string imageCollection, string seriesCollection,
        DicomLoaderOptions loadOptions, ParallelDLEHost? parallelDleHost,LoadMetadata? lmd)
    {
        _loadOptions = loadOptions;
        _parallelDleHost = parallelDleHost;
        _lmd = lmd;
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
                    if (!_loadOptions.ForceReload && ExistingEntry(path))
                    {
                        return; // Exit whole archive processing on duplicate, unless force-reloading
                    }
                    else
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

        ds.Remove(new[] { DicomTag.PixelData });
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

        _imageQueue.Enqueue((message, filtered));
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
