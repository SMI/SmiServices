using System;

namespace SmiServices.Common.Messaging;

internal static class BackoffProviderFactory
{
    public static IBackoffProvider Create(string typename)
    {
        if (!Enum.TryParse(typename, ignoreCase: true, out BackoffProviderType backoffProviderType))
            throw new ArgumentException($"Could not parse '{typename}' to a valid BackoffProviderType");

        return backoffProviderType switch
        {
            BackoffProviderType.StaticBackoffProvider => new StaticBackoffProvider(),
            BackoffProviderType.ExponentialBackoffProvider => new ExponentialBackoffProvider(),
            _ => throw new NotImplementedException($"No case for BackoffProviderType '{backoffProviderType}'"),
        };
    }
}
