using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace SIT.Manager.Services.Caching;

internal class InMemoryCachingProvider(string cachePath, ILogger<InMemoryCachingProvider> logger) : CachingProviderBase(cachePath)
{
    private const string RESTORE_FILE_NAME = "memoryCache.dat";
    protected override string RestoreFileName => RESTORE_FILE_NAME;
    private ILogger<InMemoryCachingProvider> _logger = logger;

    public override CacheValue<T> Get<T>(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));

        if (!_cacheMap.TryGetValue(key, out CacheEntry? cacheEntry))
            return CacheValue<T>.NoValue;

        if (cacheEntry.ExpiryDate < DateTime.UtcNow)
        {
            RemoveExpiredKey(key);
            return CacheValue<T>.NoValue;
        }

        try
        {
            T value = cacheEntry.GetValue<T>();
            return new CacheValue<T>(value, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occured while casting value to generic");
            return CacheValue<T>.NoValue;
        }
    }

    public override bool Add<T>(string key, T value, TimeSpan? expiryTime = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
        ArgumentNullException.ThrowIfNull(value, nameof(value));

        DateTime expiryDate = DateTime.UtcNow + (expiryTime ?? TimeSpan.FromMinutes(15));
        CacheEntry entry = new(key, value, expiryDate);
        if (expiryDate < DateTime.UtcNow)
        {
            RemoveExpiredKey(entry.Key);
            return false;
        }

        if (!_cacheMap.TryAdd(entry.Key, entry))
        {
            if (!_cacheMap.TryGetValue(entry.Key, out CacheEntry? existingEntry) || existingEntry.ExpiryDate < DateTime.UtcNow)
                return false;

            _cacheMap.AddOrUpdate(entry.Key, entry, (k, cacheEntry) => entry);
        }

        return true;
    }
}
