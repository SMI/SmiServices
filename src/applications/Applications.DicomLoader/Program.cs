using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using JetBrains.Annotations;
using Smi.Common.MongoDB;
using Smi.Common.Options;

namespace Applications.DicomLoader;

public static class Program
{
    private static CancellationTokenSource? _cts;
    public static int Main(IEnumerable<string> args)
    {
        return SmiCliInit.ParseAndRun<DicomLoaderOptions>(args, typeof(Program), OnParse);
    }

    private static int OnParse(GlobalOptions g, DicomLoaderOptions cliOptions)
    {
        return OnParse(g, cliOptions, null);
    }

    private static void CancelHandler(object? _, ConsoleCancelEventArgs a)
    {
        _cts?.Cancel();
        a.Cancel = true;
    }

    private static int OnParse(GlobalOptions go, DicomLoaderOptions dicomLoaderOptions, Stream? fileList)
    {
        Loader loader =
            new(
                MongoClientHelpers.GetMongoClient(go.MongoDatabases.DicomStoreOptions, nameof(DicomLoader))
                    .GetDatabase(go.MongoDatabases.DicomStoreOptions.DatabaseName),
                go.MongoDbPopulatorOptions.ImageCollection, go.MongoDbPopulatorOptions.SeriesCollection,dicomLoaderOptions.ForceReload);

        LineReader.LineReader fileNames = new(fileList??Console.OpenStandardInput(), '\0');
        using CancellationTokenSource cts = new();
        _cts = cts;
        Console.CancelKeyPress += CancelHandler;
        ParallelOptions parallelOptions = new()
        {
            MaxDegreeOfParallelism = -1,
            CancellationToken = cts.Token
        };
        Parallel.ForEachAsync(fileNames.ReadLines(), parallelOptions, loader.Load).Wait(cts.Token);
        Console.CancelKeyPress -= CancelHandler;
        _cts = null;
        loader.Flush(true);
        loader.Report();
        if (dicomLoaderOptions.ForceRecount)
        {
            // TODO: Implement recalculating SeriesCollection from ImageCollection
            throw new NotImplementedException();
        }
        return 0;
    }
}

[UsedImplicitly]
public class DicomLoaderOptions : CliOptions
{
    [Option(
        't',
        "totals",
        Default = false,
        Required = false,
        HelpText = "Rebuild the Mongo SeriesCollection data and image counts based on the ImageCollection contents (TODO)"
    )]
    public bool ForceRecount { get; [UsedImplicitly] set; }

    [Option(
        'r',
        "reload",
        Default = false,
        Required = false,
        HelpText = "Re-load and overwrite existing data instead of skipping previously seen files (TODO)"
    )]
    public bool ForceReload { get; [UsedImplicitly] set; }
}
