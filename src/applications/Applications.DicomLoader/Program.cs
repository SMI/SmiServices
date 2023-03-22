using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using JetBrains.Annotations;
using Microservices.DicomRelationalMapper.Execution;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Driver;
using Rdmp.Core.Curation.Data.DataLoad;
using Rdmp.Core.Curation.Data.EntityNaming;
using Rdmp.Core.Startup;
using Rdmp.Core.Startup.Events;
using ReusableLibraryCode.Checks;
using Smi.Common;
using Smi.Common.Helpers;
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
        using CancellationTokenSource cts = new();
        _cts = cts;
        Console.CancelKeyPress += CancelHandler;

        ParallelDLEHost? host = null;
        LoadMetadata? lmd = null;
        if (dicomLoaderOptions.LoadSql || dicomLoaderOptions.MatchMode)
        {
            FansiImplementations.Load();
            // Initialise a ParallelDleHost to shove the Mongo entries into SQL too:
            var rdmpRepo = go.RDMPOptions.GetRepositoryProvider();
            var startup = new Startup(new EnvironmentInfo(), rdmpRepo);
            List<Exception> errors = new();
            startup.DatabaseFound += (_, args) =>
            {
                if (args.Status == RDMPPlatformDatabaseStatus.Healthy && args.Exception is null)
                    return;
                errors.Add(args.Exception ?? new ApplicationException($"Database {args.SummariseAsString()} unhealthy state {args.Status}"));
            };
            startup.DoStartup(new ThrowImmediatelyCheckNotifier());
            if (!errors.IsNullOrEmpty())
                throw new AggregateException(errors.ToArray());
            var databaseNamerType = rdmpRepo.CatalogueRepository.MEF.GetType(go.DicomRelationalMapperOptions.DatabaseNamerType);
            if(databaseNamerType == null)
            {
                throw new Exception($"Could not find Type '{go.DicomRelationalMapperOptions.DatabaseNamerType}'");
            }

            lmd = rdmpRepo.CatalogueRepository.GetObjectByID<LoadMetadata>(go.DicomRelationalMapperOptions.LoadMetadataId);
            var liveDatabaseName = lmd.GetDistinctLiveDatabaseServer().GetCurrentDatabase().GetRuntimeName();
            var instance = new MicroserviceObjectFactory().CreateInstance<INameDatabasesAndTablesDuringLoads>(databaseNamerType, liveDatabaseName, go.DicomRelationalMapperOptions.Guid);
            host = new ParallelDLEHost(rdmpRepo,instance,true);
        }

        // MatchMode: reconcile Mongo contents with SQL:
        var mongo = MongoClientHelpers.GetMongoClient(go.MongoDatabases.DicomStoreOptions, nameof(DicomLoader))
            .GetDatabase(go.MongoDatabases.DicomStoreOptions.DatabaseName);
        if (dicomLoaderOptions.MatchMode)
        {
            var findOptions = new FindOptions<BsonDocument,BsonDocument>
            {
                Sort = "header.DicomFilePath"
            };
            mongo.GetCollection<BsonDocument>(go.MongoDbPopulatorOptions.ImageCollection).FindSync("",findOptions,cts.Token);
            return 0;
        }

        Loader loader =
            new(
                mongo,
                go.MongoDbPopulatorOptions.ImageCollection, go.MongoDbPopulatorOptions.SeriesCollection,dicomLoaderOptions,host,lmd);

        LineReader.LineReader fileNames = new(fileList??Console.OpenStandardInput(), '\0');
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

[UsedImplicitly]
public class DicomLoaderOptions : CliOptions
{
<<<<<<< HEAD
    [Option('m', "memoryLimit", Default = 16, Required = false, HelpText = "Memory threshold to flush in GiB")]
    public long MemoryLimit
    {
        get;
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
        set;
    } = 16;

    [Option('p', "parallelism", Default = -1, Required = false, HelpText = "Number of threads to run in parallel")]
    public int Parallelism
    {
        get;
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
        set;
    } = -1;
=======
    [Option('m',"match",Default = false,Required = false,HelpText = "Match Mongo against SQL")]
    public bool MatchMode { get; set; }

    [Option(
        'r',
        "reload",
        Default = false,
        Required = false,
        HelpText = "Re-load and overwrite existing data instead of skipping previously seen files (TODO)"
    )]
    public bool ForceReload { get; [UsedImplicitly] set; }

>>>>>>> 3b98ed23 (Stub implementation of MatchMode for SQL-Mongo reconciliation)

    [Option('s',"sql",Default = false,Required = false,HelpText = "Load data on to the SQL stage after Mongo")]
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
