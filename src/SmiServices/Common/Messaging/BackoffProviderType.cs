namespace SmiServices.Common.Messaging;

public enum BackoffProviderType
{
    None = 0,
    StaticBackoffProvider,
    ExponentialBackoffProvider,
}
