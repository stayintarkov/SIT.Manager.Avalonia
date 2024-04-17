using Avalonia.Controls.ApplicationLifetimes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace SIT.Manager.Services.Caching;

public class InMemoryCachingService : ICachingProvider
{
    private const string RESTORE_FILE_NAME = "memoryCache.dat";
    private readonly ConcurrentDictionary<string, CacheEntry> _memoryCache = new();
    private readonly DirectoryInfo _cachePath;
    public event EventHandler<EvictedEventArgs>? Evicted;

    public InMemoryCachingService(string cachePath)
    {
        _cachePath = new(cachePath);
        _cachePath.Create();

        string restoreFilePath = Path.Combine(_cachePath.FullName, RESTORE_FILE_NAME);
        if (File.Exists(restoreFilePath))
        {
            _memoryCache = JsonSerializer.Deserialize<ConcurrentDictionary<string, CacheEntry>>(File.ReadAllText(restoreFilePath)) ?? new();
        }
        else
        {
            _memoryCache = new();
        }

        if(App.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            lifetime.ShutdownRequested += (sender, e) =>
            {
                SaveKeysToFile();
            };
        }
    }

    private void SaveKeysToFile()
    {
        if (_memoryCache.IsEmpty)
            return;

        string keyDataPath = Path.Combine(_cachePath.FullName, RESTORE_FILE_NAME);
        File.WriteAllText(keyDataPath, JsonSerializer.Serialize(_memoryCache));
    }

    public void Clear(string prefix = "")
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            _memoryCache.Clear();
        }
        else
        {
            IEnumerable<string> prefixedCachedKeys = _memoryCache.Keys.Where(x => x.StartsWith(prefix));
            foreach (string key in prefixedCachedKeys)
            {
                _memoryCache.TryRemove(key, out _);
            }
        }
    }

    public int GetCount(string prefix = "")
    {
        IEnumerable<CacheEntry> cacheItems = cacheItems = _memoryCache.Values.Where(x => x.ExpiryDate > DateTime.UtcNow);
        if (!string.IsNullOrWhiteSpace(prefix))
        {
            cacheItems = cacheItems.Where(x => x.Key.StartsWith(prefix));
        }

        return cacheItems.Count();
    }

    internal void RemoveExpiredKey(string key)
    {
        if (_memoryCache.TryRemove(key, out _))
        {
            Evicted?.Invoke(this, new EvictedEventArgs(key));
        }
    }

    public CacheValue<T> Get<T>(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));

        if (!_memoryCache.TryGetValue(key, out CacheEntry? cacheEntry))
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
            //TODO: log exception here
            return CacheValue<T>.NoValue;
        }
    }

    public bool TryGet<T>(string key, out CacheValue<T> cacheValue)
    {
        cacheValue = Get<T>(key);
        if (cacheValue == CacheValue<T>.NoValue || cacheValue == CacheValue<T>.Null)
            return false;
        return true;
    }

    public CacheValue<T> GetOrCompute<T>(string key, Func<string, T> computor, TimeSpan? expiaryTime = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));

        bool success = TryGet(key, out CacheValue<T> valOut);
        if (success)
            return valOut;

        T computedValue = computor(key);
        bool addSuccess = Add(key, computedValue, expiaryTime);

        if (!addSuccess)
            throw new Exception("Cached value did not exist but could not be added to the cache");

        return Get<T>(key);
    }

    public bool Add<T>(string key, T value, TimeSpan? expiryTime = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
        ArgumentNullException.ThrowIfNull(value, nameof(value));

        DateTime expiryDate = DateTime.UtcNow + (expiryTime ?? TimeSpan.FromMinutes(15));
        return SetInternal(new CacheEntry(key, value, expiryDate));
    }

    private bool SetInternal(CacheEntry entry, bool addOnly = false)
    {
        if (entry.ExpiryDate < DateTime.UtcNow)
        {
            RemoveExpiredKey(entry.Key);
            return false;
        }

        if (addOnly)
        {
            if (!_memoryCache.TryAdd(entry.Key, entry))
            {
                if (!_memoryCache.TryGetValue(entry.Key, out CacheEntry? existingEntry) || existingEntry.ExpiryDate < DateTime.UtcNow)
                    return false;

                _memoryCache.AddOrUpdate(entry.Key, entry, (k, cacheEntry) => entry);
            }
        }
        else
        {
            _memoryCache.AddOrUpdate(entry.Key, entry, (k, cacheEntry) => entry);
        }

        return true;
    }

    public bool Exists(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
        return _memoryCache.TryGetValue(key, out CacheEntry? entry) && entry.ExpiryDate > DateTime.UtcNow;
    }

    public bool Remove(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
        return _memoryCache.TryRemove(key, out _);
    }

    public int RemoveByPrefix(string prefix)
    {
        var keysToRemove = _memoryCache.Keys.Where(x => x.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
        int removed = 0;
        foreach (var key in keysToRemove)
        {
            if (Remove(key))
                removed++;
        }
        return removed;
    }

    public IEnumerable<string> GetAllKeys(string prefix)
    {
        return _memoryCache.Values
            .Where(x => x.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && x.ExpiryDate > DateTime.UtcNow)
            .Select(x => x.Key).ToList();
    }
}
