using Smi.Common.Options;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Applications.SmiRunner
{
    public static class Program
    {
        public static readonly Type[] AllApplications =
        {
            typeof(DicomDirectoryProcessorVerb),
            typeof(ExtractImagesVerb),
            typeof(IsIdentifiableReviewerVerb),
            typeof(TriggerUpdatesVerb),
        };

        public static readonly Type[] AllServices =
        {
            typeof(CohortExtractorVerb),
            typeof(DicomAnonymiserVerb),
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
        };

        internal static int Main(string[] args)
        {
            IEnumerable<string> rest = args.Skip(1);

            var allTypes = new List<Type>();
            allTypes.AddRange(AllApplications);
            allTypes.AddRange(AllServices);

            int res;

            try
            {
                res = SmiCliInit.ParseServiceVerbAndRun(
                    args.Take(1),
                    allTypes.ToArray(),
                    service =>
                    {
                        // TODO(rkm 2021-02-26) Probably want to test that every case is covered here
                        return service switch
                        {
                            // Applications
                            TriggerUpdatesVerb _ => Applications.TriggerUpdates.Program.Main(rest),
                            DicomDirectoryProcessorVerb _ => Applications.DicomDirectoryProcessor.Program.Main(rest),
                            IsIdentifiableReviewerVerb _ => Applications.IsIdentifiableReviewer.Program.Main(rest),
                            ExtractImagesVerb _ => ExtractImages.Program.Main(rest),
                            // Microservices
                            CohortExtractorVerb _ => Microservices.CohortExtractor.Program.Main(rest),
                            CohortPackagerVerb _ => Microservices.CohortPackager.Program.Main(rest),
                            DeadLetterReprocessorVerb _ => Microservices.DeadLetterReprocessor.Program.Main(rest),
                            DicomAnonymiserVerb _ => Microservices.DicomAnonymiser.Program.Main(rest),
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
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                const int rc = 1;
                Console.Error.WriteLine($"\nError (exit code {rc}): {e.Message}");
                return rc;
            }

            if(args.Any(a=>a.Equals("--help")))
            {
                Console.WriteLine("Read more at:");
                Console.WriteLine("https://github.com/SMI/SmiServices/tree/master/");
            }
               
            return res;
        }
    }
}
