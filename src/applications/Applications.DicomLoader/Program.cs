using JetBrains.Annotations;
using MongoDB.Bson;
using Smi.Common.Messages;
using Smi.Common.MongoDB;
using Smi.Common.Options;

namespace Applications.DicomLoader;

public static class Program
{
    public static int Main(IEnumerable<string> args)
    {
        return SmiCliInit.ParseAndRun<DicomLoaderOptions>(args, typeof(Program), OnParse);
    }

    private static int OnParse(GlobalOptions g, DicomLoaderOptions d)
    {
        return OnParse(g, d, null);
    }

    // ReSharper disable once UnusedParameter.Global
    public static int OnParse(GlobalOptions _, DicomLoaderOptions dicomLoaderOptions,Stream? fileList)
    {
        if (dicomLoaderOptions.Database is null) throw new ArgumentNullException(nameof(dicomLoaderOptions.Database));
        Loader.Database=MongoClientHelpers.GetMongoClient(dicomLoaderOptions.Database, nameof(DicomLoader)).GetDatabase(dicomLoaderOptions.Database.DatabaseName);
        Loader.ImageStore = Loader.Database.GetCollection<BsonDocument>(dicomLoaderOptions.ImageCollection);
        Loader.SeriesStore = Loader.Database.GetCollection<SeriesMessage>(dicomLoaderOptions.SeriesCollection);

        LineReader.LineReader fileNames = new(fileList??Console.OpenStandardInput(), '\0');
        CancellationTokenSource cts = new();
        Console.CancelKeyPress += (_, e) =>
        {
            cts.Cancel();
            e.Cancel = true;
        };
        ParallelOptions parallelOptions = new()
        {
            MaxDegreeOfParallelism = -1,
            CancellationToken = cts.Token
        };
        Parallel.ForEachAsync(fileNames.ReadLines(), parallelOptions, Loader.Load).Wait(cts.Token);
        Loader.Flush(true);
        Loader.Report();
        return 0;
    }
}

[UsedImplicitly]
public class DicomLoaderOptions : CliOptions
{
    public MongoDbOptions? Database { get; set; }
    public string? ImageCollection { get; [UsedImplicitly] set; }
    public string? SeriesCollection { get; [UsedImplicitly] set; }
}