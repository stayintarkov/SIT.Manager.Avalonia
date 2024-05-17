using Avalonia.Controls.ApplicationLifetimes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SIT.Manager.Services.Caching;
internal abstract class CachingProviderBase : ICachingProvider
{
    protected readonly ConcurrentDictionary<string, CacheEntry> _cacheMap = new();
    protected readonly DirectoryInfo _cachePath;
    private readonly Timer _landlord;
    protected abstract string RestoreFileName { get; }
    public event EventHandler<EvictedEventArgs>? Evicted;

    protected CachingProviderBase(string cachePath)
    {
        _cachePath = new DirectoryInfo(cachePath);
        _cachePath.Create();
        _landlord = new Timer(EvictTenants, _cacheMap, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(0.5));

        string restoreFilePath = Path.Combine(cachePath, RestoreFileName);
        if (File.Exists(restoreFilePath))
            _cacheMap = JsonSerializer.Deserialize<ConcurrentDictionary<string, CacheEntry>>(File.ReadAllText(restoreFilePath)) ?? new();

        if (App.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            lifetime.ShutdownRequested += (sender, e) =>
            {
                CleanCache();
                SaveKeysToFile(RestoreFileName);
            };
        }
    }

    protected virtual void CleanCache()
    {
        EvictTenants(_cacheMap);
    }

    protected virtual void SaveKeysToFile(string restoreFileName)
    {
        _cachePath.Create();
        string keyDataPath = Path.Combine(_cachePath.FullName, restoreFileName);
        if (_cacheMap.IsEmpty)
        {
            if (File.Exists(keyDataPath))
                File.Delete(keyDataPath);
            return;
        }
        File.WriteAllText(keyDataPath, JsonSerializer.Serialize(_cacheMap));
    }
    protected virtual void EvictTenants(object? state)
    {
        if (state == null)
            return;

        ConcurrentDictionary<string, CacheEntry> cache = (ConcurrentDictionary<string, CacheEntry>) state;
        foreach (CacheEntry entry in cache.Values)
        {
            if (entry.ExpiryDate >= DateTime.UtcNow)
                continue;

            if (Remove(entry.Key))
                Evicted?.Invoke(this, new EvictedEventArgs(entry.Key));
        }

        SaveKeysToFile(RestoreFileName);
    }
    public virtual void Clear(string prefix = "")
    {
        IEnumerable<CacheEntry> entriesToRemove = string.IsNullOrWhiteSpace(prefix) ? _cacheMap.Values : _cacheMap.Values.Where(x => x.Key.StartsWith(prefix));

        foreach (CacheEntry entry in entriesToRemove)
        {
            string filePath = entry.Value as string ?? string.Empty;
            if (File.Exists(filePath))
                File.Delete(filePath);
            _cacheMap.Remove(entry.Key, out _);
        }
    }
    public virtual bool Exists(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
        return _cacheMap.TryGetValue(key, out CacheEntry? entry) && entry.ExpiryDate > DateTime.UtcNow;
    }
    public virtual IEnumerable<string> GetAllKeys(string prefix)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        return _cacheMap.Values
            .Where(x => x.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && x.ExpiryDate > DateTime.UtcNow)
            .Select(x => x.Key).ToList();
    }
    public virtual int GetCount(string prefix = "")
    {
        IEnumerable<CacheEntry> cacheItems = cacheItems = _cacheMap.Values.Where(x => x.ExpiryDate > DateTime.UtcNow);
        if (!string.IsNullOrWhiteSpace(prefix))
        {
            cacheItems = cacheItems.Where(x => x.Key.StartsWith(prefix));
        }

        return cacheItems.Count();
    }
    public virtual bool Remove(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
        return _cacheMap.TryRemove(key, out _);
    }
    public virtual int RemoveByPrefix(string prefix)
    {
        var keysToRemove =
            _cacheMap.Keys.Where(x => x.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
        return keysToRemove.Count(Remove);
    }
    public virtual CacheValue<T> GetOrCompute<T>(string key, Func<string, T> computor, TimeSpan? expiryTime = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));

        bool success = TryGet(key, out CacheValue<T> valOut);
        if (success)
            return valOut;

        T computedValue = computor(key);
        bool addSuccess = Add(key, computedValue, expiryTime);

        if (!addSuccess)
            throw new Exception("Cached value did not exist but could not be added to the cache");

        return Get<T>(key);
    }

    //TODO: Make this based off synchro version
    public virtual async Task<CacheValue<T>> GetOrComputeAsync<T>(string key, Func<string, Task<T>> computor, TimeSpan? expiryTime = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));

        bool success = TryGet(key, out CacheValue<T> valOut);
        if (success)
            return valOut;

        T computedValue = await computor(key);
        bool addSuccess = Add(key, computedValue, expiryTime);

        if (!addSuccess)
            throw new Exception("Cached value did not exist but could not be added to the cache");

        return Get<T>(key);
    }
    public virtual bool TryGet<T>(string key, out CacheValue<T> cacheValue)
    {
        cacheValue = Get<T>(key);
        return cacheValue != CacheValue<T>.NoValue;
    }

    protected virtual void OnEvictedTenant(EvictedEventArgs e)
    {
        Evicted?.Invoke(this, e);
    }
    public abstract bool Add<T>(string key, T value, TimeSpan? expiryTime = null);
    public abstract CacheValue<T> Get<T>(string key);
}
