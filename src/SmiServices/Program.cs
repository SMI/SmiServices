using SmiServices.Common.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;


namespace SmiServices;

[ExcludeFromCodeCoverage]
public static class Program
{
    public static readonly Type[] AllApplications =
    [
        typeof(DicomLoaderVerb),
        typeof(DicomDirectoryProcessorVerb),
        typeof(ExtractImagesVerb),
        typeof(TriggerUpdatesVerb),
        typeof(SetupVerb),
        typeof(DynamicRulesTesterVerb),
    ];

    public static readonly Type[] AllServices =
    [
        typeof(CohortExtractorVerb),
        typeof(DicomAnonymiserVerb),
        typeof(CohortPackagerVerb),
        typeof(DicomRelationalMapperVerb),
        typeof(DicomReprocessorVerb),
        typeof(DicomTagReaderVerb),
        typeof(FileCopierVerb),
        typeof(IdentifierMapperVerb),
        typeof(IsIdentifiableVerb),
        typeof(MongoDbPopulatorVerb),
        typeof(UpdateValuesVerb),
    ];

    public static int Main(string[] args)
    {
        var rest = args.Skip(1);

        var allTypes = new List<Type>(AllApplications);
        allTypes.AddRange(AllServices);

        int res;

        try
        {
            res = SmiCliInit.ParseServiceVerbAndRun(
                args.Take(1),
                [.. allTypes],
                service =>
                {
                    // TODO(rkm 2021-02-26) Probably want to test that every case is covered here
                    return service switch
                    {
                        // Applications
                        DicomLoaderVerb _ => Applications.DicomLoader.DicomLoader.Main(rest),
                        DynamicRulesTesterVerb _ => Applications.DynamicRulesTester.DynamicRulesTester.Main(rest),
                        TriggerUpdatesVerb _ => Applications.TriggerUpdates.TriggerUpdates.Main(rest),
                        DicomDirectoryProcessorVerb _ => Applications.DicomDirectoryProcessor.DicomDirectoryProcessor.Main(rest),
                        ExtractImagesVerb _ => Applications.ExtractImages.ExtractImages.Main(rest),
                        SetupVerb _ => Applications.Setup.Setup.Main(rest),

                        // Microservices
                        CohortExtractorVerb _ => Microservices.CohortExtractor.CohortExtractor.Main(rest),
                        CohortPackagerVerb _ => Microservices.CohortPackager.CohortPackager.Main(rest),
                        DicomAnonymiserVerb _ => Microservices.DicomAnonymiser.DicomAnonymiser.Main(rest),
                        DicomRelationalMapperVerb _ => Microservices.DicomRelationalMapper.DicomRelationalMapper.Main(rest),
                        DicomReprocessorVerb _ => Microservices.DicomReprocessor.DicomReprocessor.Main(rest),
                        DicomTagReaderVerb _ => Microservices.DicomTagReader.DicomTagReader.Main(rest),
                        FileCopierVerb _ => Microservices.FileCopier.FileCopier.Main(rest),
                        IdentifierMapperVerb _ => Microservices.IdentifierMapper.IdentifierMapper.Main(rest),
                        IsIdentifiableVerb _ => Microservices.IsIdentifiable.IsIdentifiable.Main(rest),
                        MongoDbPopulatorVerb _ => Microservices.MongoDBPopulator.MongoDBPopulator.Main(rest),
                        UpdateValuesVerb _ => Microservices.UpdateValues.UpdateValues.Main(rest),
                        _ => throw new ArgumentException($"No case for {nameof(service)}")
                    };
                }
            );
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            const int rc = 1;
            Console.Error.WriteLine($"\nError (exit code {rc}): {e.Message}");
            return rc;
        }

        if (args.Any(a => a.Equals("--help")))
        {
            Console.WriteLine("Read more at:");
            Console.WriteLine("https://github.com/SMI/SmiServices/tree/main/");
        }

        return res;
    }
}
