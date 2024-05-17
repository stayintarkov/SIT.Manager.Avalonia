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
            if(Remove(cacheEntry.Key))
                OnEvictedTenant(new EvictedEventArgs(cacheEntry.Key));
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
        CacheEntry cacheEntry = new(key, value, expiryDate);
        if (expiryDate < DateTime.UtcNow)
        {
            if(Remove(cacheEntry.Key))
                OnEvictedTenant(new EvictedEventArgs(cacheEntry.Key));
            return false;
        }

        if (_cacheMap.TryAdd(cacheEntry.Key, cacheEntry))
            return true;

        if (!_cacheMap.TryGetValue(cacheEntry.Key, out CacheEntry? existingEntry) || existingEntry.ExpiryDate < DateTime.UtcNow)
            return false;

        _cacheMap.AddOrUpdate(cacheEntry.Key, cacheEntry, (_, _) => cacheEntry);

        return true;
    }

    protected override void SaveKeysToFile(string restoreFileName)
    {
        //TODO: Implement me!
    }
}
