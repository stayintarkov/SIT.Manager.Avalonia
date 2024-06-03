using Microsoft.Extensions.Logging;
using System;

namespace SIT.Manager.Services.Caching;

internal class InMemoryCachingProvider(ILogger<InMemoryCachingProvider> logger) : CachingProviderBase
{
    public override CacheValue<T> Get<T>(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));

        if (!TryGetCacheEntry(key, out CacheEntry? cacheEntry)) return CacheValue<T>.NoValue;

        try
        {
            T value = cacheEntry.GetValue<T>();
            return new CacheValue<T>(value, true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occured while casting value to generic");
            return CacheValue<T>.NoValue;
        }
    }
}
