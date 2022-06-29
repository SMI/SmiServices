using Rdmp.Core.Startup;
using Smi.Common.Options;
using System;

namespace Microservices.DicomAnonymiser.Anonymisers
{
    public static class AnonymiserFactory
    {
        public static IDicomAnonymiser CreateAnonymiser(GlobalOptions globals, DicomAnonymiserOptions dicomAnonymiserOptions)
        {
            var anonymiserTypeStr = dicomAnonymiserOptions.AnonymiserType;

            if(anonymiserTypeStr.StartsWith(nameof(RdmpFoDicomAnonymiser)))
            {
                var tokens = anonymiserTypeStr.Split(":");
                if (tokens.Length != 2 || !int.TryParse(tokens[1], out _))
                {
                    throw new Exception("Expected a type string in the format 'RdmpFoDicomAnonymiser:134' where the number indicates the ID of the anonymiser PipelineComponent in RDMP");
                }


                var repo = new LinkedRepositoryProvider(
                globals.RDMPOptions.CatalogueConnectionString,
                globals.RDMPOptions.DataExportConnectionString);

                return new RdmpFoDicomAnonymiser(repo,int.Parse(tokens[1]));
            }

            if (!Enum.TryParse(anonymiserTypeStr, ignoreCase: true, out AnonymiserType anonymiserType))
                throw new ArgumentException($"Could not parse '{anonymiserTypeStr}' to a valid AnonymiserType");

            return anonymiserType switch
            {
                // TODO(rkm 2021-12-07) Can remove the LGTM ignore once an AnonymiserType is implemented
                _ => throw new NotImplementedException($"No case for AnonymiserType '{anonymiserType}'"), // lgtm[cs/constant-condition]
            };
        }
    }
}
