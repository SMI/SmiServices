using CommandLine;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Driver;
using Rdmp.Core.Curation.Data.DataLoad;
using Rdmp.Core.Curation.Data.EntityNaming;
using Rdmp.Core.Repositories;
using Rdmp.Core.ReusableLibraryCode.Annotations;
using Rdmp.Core.ReusableLibraryCode.Checks;
using Rdmp.Core.Startup;
using Rdmp.Core.Startup.Events;
using SmiServices.Common;
using SmiServices.Common.Helpers;
using SmiServices.Common.MongoDB;
using SmiServices.Common.Options;
using SmiServices.Microservices.DicomRelationalMapper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;

namespace SmiServices.Applications.DicomLoader;

public static class DicomLoader
{
    private static CancellationTokenSource? _cts;

    [ExcludeFromCodeCoverage]
    public static int Main(IEnumerable<string> args)
    {
        return SmiCliInit.ParseAndRun<DicomLoaderOptions>(args, nameof(DicomLoader), OnParse);
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
        if (go.MongoDatabases?.DicomStoreOptions is null)
            throw new InvalidOperationException("MongoDatabases or DICOM store options not set");
        if (go.MongoDbPopulatorOptions?.ImageCollection is null || go.MongoDbPopulatorOptions?.SeriesCollection is null)
            throw new InvalidOperationException("MongoDbPopulatorOptions not set");
        if (go.RDMPOptions is null)
            throw new InvalidOperationException("RDMPOptions not set");

        using CancellationTokenSource cts = new();
        _cts = cts;
        Console.CancelKeyPress += CancelHandler;

        ParallelDLEHost? host = null;
        LoadMetadata? lmd = null;
        var mongo = MongoClientHelpers.GetMongoClient(go.MongoDatabases.DicomStoreOptions, nameof(DicomLoader), false, true)
            .GetDatabase(go.MongoDatabases.DicomStoreOptions.DatabaseName);
        if (dicomLoaderOptions.LoadSql || dicomLoaderOptions.MatchMode != null)
        {
            FansiImplementations.Load();
            // Initialise a ParallelDleHost to shove the Mongo entries into SQL too:
            var rdmpRepo = go.RDMPOptions.GetRepositoryProvider();
            var startup = new Startup(rdmpRepo)
            {
                SkipPatching = true
            };
            List<Exception> errors = [];
            startup.DatabaseFound += (_, args) =>
            {
                if (args.Status == RDMPPlatformDatabaseStatus.Healthy && args.Exception is null)
                    return;

                errors.Add(args.Exception ?? new ApplicationException($"Database {args.SummariseAsString()} unhealthy state {args.Status}"));
            };
            startup.DoStartup(ThrowImmediatelyCheckNotifier.Quiet);
            if (!errors.IsNullOrEmpty())
                throw new AggregateException([.. errors]);

            var databaseNamerType = MEF.GetType(go.DicomRelationalMapperOptions?.DatabaseNamerType) ??
                                    throw new Exception($"Could not find Type '{go.DicomRelationalMapperOptions?.DatabaseNamerType}'");

            lmd = rdmpRepo.CatalogueRepository.GetObjectByID<LoadMetadata>(
                go.DicomRelationalMapperOptions?.LoadMetadataId ??
                throw new InvalidOperationException("DicomRelationalMapper LoadMetadataId not set"));
            var liveDatabaseName = lmd.GetDistinctLiveDatabaseServer().GetCurrentDatabase()?.GetRuntimeName() ??
                                   throw new ApplicationException("No database found");
            var instance =
                new MicroserviceObjectFactory().CreateInstance<INameDatabasesAndTablesDuringLoads>(databaseNamerType,
                    liveDatabaseName, go.DicomRelationalMapperOptions.Guid) ??
                throw new InvalidOperationException($"Failed to instantiate {nameof(INameDatabasesAndTablesDuringLoads)}");
            host = new ParallelDLEHost(rdmpRepo, instance, true);

            // MatchMode: reconcile Mongo contents with SQL:
            if (dicomLoaderOptions.MatchMode != null)
            {
                var findOptions = new FindOptions<BsonDocument, BsonDocument>
                {
                    Sort = "{\"header.DicomFilePath\":1}"
                };
                using var cursor = mongo.GetCollection<BsonDocument>(go.MongoDbPopulatorOptions.ImageCollection).FindSync(dicomLoaderOptions.MatchMode, findOptions, cts.Token);
                Loader.FlushRelational(host ?? throw new InvalidOperationException(),
                    lmd ?? throw new InvalidOperationException(),
                    cursor.ToEnumerable(cts.Token).Select(Loader.ParseBson));
                return 0;
            }
        }

        Loader loader =
            new(
                mongo,
                go.MongoDbPopulatorOptions.ImageCollection, go.MongoDbPopulatorOptions.SeriesCollection, dicomLoaderOptions, host, lmd);
        LineReader.LineReader fileNames = new(fileList ?? Console.OpenStandardInput(), '\0');
        ParallelOptions parallelOptions = new()
        {
            MaxDegreeOfParallelism = dicomLoaderOptions.Parallelism,
            CancellationToken = cts.Token
        };
        Parallel.ForEachAsync(fileNames.ReadLines(), parallelOptions, loader.Load).Wait(cts.Token);
        Console.CancelKeyPress -= CancelHandler;
        _cts = null;
        loader.Flush();
        loader.Report();
        if (dicomLoaderOptions.ForceRecount)
        {
            // TODO: Implement recalculating SeriesCollection from ImageCollection
            throw new NotImplementedException();
        }

        return 0;
    }
}

public class DicomLoaderOptions : CliOptions
{
    [Option(
        'd',
        "delete",
        Default = false,
        Required = false,
        HelpText = "Delete existing data instead of skipping previously seen files"
    )]
    public bool DeleteConflicts { get; set; }

    [Option('p', "parallelism", Default = -1, Required = false, HelpText = "Number of threads to run in parallel")]
    public int Parallelism
    {
        get;
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
        set;
    } = -1;

    [Option('m', "match", Default = null, Required = false, HelpText = "Copy matching Mongo entries to SQL and exit")]
    public string? MatchMode { get; set; }

    [Option('r', "ramLimit", Default = 16, Required = false, HelpText = "RAM threshold to flush in GiB")]
    public long RamLimit
    {
        get;
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
        set;
    } = 16;

    [Option('s', "sql", Default = false, Required = false, HelpText = "Load data on to the SQL stage after Mongo")]
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public bool LoadSql { get; set; }

    [Option(
        't',
        "totals",
        Default = false,
        Required = false,
        HelpText = "Rebuild the Mongo SeriesCollection data and image counts based on the ImageCollection contents (TODO)"
    )]
    public bool ForceRecount { get; [UsedImplicitly] set; }
}
