using SmiServices.Common.Options;
using System;

namespace SmiServices.Microservices.DicomAnonymiser.Anonymisers;

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
            AnonymiserType.SmiCtpAnonymiser => new SmiCtpAnonymiser(options),
            _ => throw new NotImplementedException($"No case for AnonymiserType '{anonymiserType}'"),
        };
    }
}
