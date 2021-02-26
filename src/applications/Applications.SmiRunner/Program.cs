using System;
using System.Collections.Generic;
using System.Linq;
using Smi.Common.Options;


namespace Applications.SmiRunner
{
    public static class Program
    {
        internal static int Main(string[] args)
        {
            IEnumerable<string> rest = args.Skip(1);

            // TODO probably want some tests to ensure the below includes all apps/services
            int res = SmiCliInit.ParseServiceVerbAndRun(
                args.Take(1),
                new[]
                {
                    // Applications
                    typeof(TriggerUpdatesVerb),
                    typeof(DicomDirectoryProcessorVerb),
                    typeof(IsIdentifiableReviewerVerb),
                    // Microservices
                    typeof(CohortExtractorVerb),
                    typeof(CohortPackagerVerb),
                    typeof(DeadLetterReprocessorVerb),
                    typeof(DicomRelationalMapperVerb),
                    typeof(DicomReprocessorVerb),
                    typeof(DicomTagReaderVerb),
                    typeof(FileCopierVerb),
                    typeof(IdentifierMapperVerb),
                    typeof(IsIdentifiableVerb),
                    typeof(MongoDbPopulatorVerb),
                    typeof(UpdateValuesVerb),
                },
                service =>
                {
                    return service switch
                    {
                        // DicomTagReaderVerb _ => Microservices.DicomTagReader.Program.Main(rest),
                        // Applications
                        TriggerUpdatesVerb _ => Applications.TriggerUpdates.Program.Main(rest),
                        DicomDirectoryProcessorVerb _ => Applications.DicomDirectoryProcessor.Program.Main(rest),
                        IsIdentifiableReviewerVerb _ => Applications.IsIdentifiableReviewer.Program.Main(rest),
                        // Microservices
                        CohortExtractorVerb _ => Microservices.CohortExtractor.Program.Main(rest),
                        CohortPackagerVerb _ => Microservices.CohortPackager.Program.Main(rest),
                        DeadLetterReprocessorVerb _ => Microservices.DeadLetterReprocessor.Program.Main(rest),
                        DicomRelationalMapperVerb _ => Microservices.DicomRelationalMapper.Program.Main(rest),
                        DicomReprocessorVerb _ => Microservices.DicomReprocessor.Program.Main(rest),
                        DicomTagReaderVerb _ => Microservices.DicomTagReader.Program.Main(rest),
                        FileCopierVerb _ => Microservices.FileCopier.Program.Main(rest),
                        IdentifierMapperVerb _ => Microservices.IdentifierMapper.Program.Main(rest),
                        IsIdentifiableVerb _ => Microservices.IsIdentifiable.Program.Main(rest),
                        MongoDbPopulatorVerb _ => Microservices.MongoDBPopulator.Program.Main(rest),
                        UpdateValuesVerb _ => Microservices.UpdateValues.Program.Main(rest),
                        _ => throw new ArgumentException($"No case for {nameof(service)}")
                    };
                }
            );
            return res;
        }
    }
}
