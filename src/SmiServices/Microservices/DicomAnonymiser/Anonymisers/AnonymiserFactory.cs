using SmiServices.Common.Options;
using System;

namespace SmiServices.Microservices.DicomAnonymiser.Anonymisers
{
    public static class AnonymiserFactory
    {
        public static IDicomAnonymiser CreateAnonymiser(GlobalOptions options)
        {
            var anonymiserTypeStr = options.DicomAnonymiserOptions!.AnonymiserType;
            if (!Enum.TryParse(anonymiserTypeStr, ignoreCase: true, out AnonymiserType anonymiserType))
                throw new ArgumentException($"Could not parse '{anonymiserTypeStr}' to a valid AnonymiserType");

            return anonymiserType switch
            {
                AnonymiserType.DefaultAnonymiser => new DefaultAnonymiser(options),
                // TODO(rkm 2021-12-07) Can remove the LGTM ignore once an AnonymiserType is implemented
                _ => throw new NotImplementedException($"No case for AnonymiserType '{anonymiserType}'"), // lgtm[cs/constant-condition]
            };
        }
    }
}
