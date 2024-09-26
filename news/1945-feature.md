Add a publish timeout backoff mechanism to ProducerModel, allowing control over message publishing timeout behaviour. This can be enabled by setting `BackoffProviderType` in any `ProducerOptions` config. Currently implemented types are:
- StaticBackoffProvider (1 minute flat timeout)
- ExponentialBackoffProvider (1 minute initial, doubling after each timeout)