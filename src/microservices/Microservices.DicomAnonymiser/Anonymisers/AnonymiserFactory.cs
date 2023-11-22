using Smi.Common.Options;
using System;

namespace Microservices.DicomAnonymiser.Anonymisers
{
    public static class AnonymiserFactory
    {
        public static IDicomAnonymiser CreateAnonymiser(DicomAnonymiserOptions dicomAnonymiserOptions)
        {
            var anonymiserTypeStr = dicomAnonymiserOptions.AnonymiserType;
            if (!Enum.TryParse(anonymiserTypeStr, ignoreCase: true, out AnonymiserType anonymiserType))
                throw new ArgumentException($"Could not parse '{anonymiserTypeStr}' to a valid AnonymiserType");

            return anonymiserType switch
            {
                AnonymiserType.DicomAnonymiser => new DicomAnonymiser(),
                // TODO(rkm 2021-12-07) Can remove the LGTM ignore once an AnonymiserType is implemented
                _ => throw new NotImplementedException($"No case for AnonymiserType '{anonymiserType}'"), // lgtm[cs/constant-condition]
            };
        }
    }
}
