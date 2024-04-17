using Avalonia.Controls.ApplicationLifetimes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
        _cachePath = new(cachePath);
        _landlord = new(new TimerCallback(EvictTenents), _cacheMap, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(10));

        if (App.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            lifetime.ShutdownRequested += (sender, e) =>
            {
                SaveKeysToFile(RestoreFileName);
            };
        }
    }

    protected void SaveKeysToFile(string restoreFileName)
    {
        if (_cacheMap.IsEmpty)
            return;

        string keyDataPath = Path.Combine(_cachePath.FullName, restoreFileName);
        File.WriteAllText(keyDataPath, JsonSerializer.Serialize(_cacheMap));
    }
    private void EvictTenents(object? state)
    {
        if (state == null)
            return;

        ConcurrentDictionary<string, CacheEntry> cache = (ConcurrentDictionary<string, CacheEntry>)state;
        foreach(CacheEntry entry in cache.Values)
        {
            if(entry.ExpiryDate <  DateTime.UtcNow)
                RemoveExpiredKey(entry.Key);
        }

        SaveKeysToFile(RestoreFileName);
    }

    protected void RemoveExpiredKey(string key)
    {
        if (_cacheMap.TryRemove(key, out _))
        {
            Evicted?.Invoke(this, new EvictedEventArgs(key));
        }
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
        var keysToRemove = _cacheMap.Keys.Where(x => x.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
        int removed = 0;
        foreach (var key in keysToRemove)
        {
            if (Remove(key))
                removed++;
        }
        return removed;
    }
    public virtual CacheValue<T> GetOrCompute<T>(string key, Func<string, T> computor, TimeSpan? expiaryTime = null)
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
    public virtual bool TryGet<T>(string key, out CacheValue<T> cacheValue)
    {
        cacheValue = Get<T>(key);
        if (cacheValue == CacheValue<T>.NoValue || cacheValue == CacheValue<T>.Null)
            return false;
        return true;
    }
    public abstract bool Add<T>(string key, T value, TimeSpan? expiryTime = null);
    public abstract CacheValue<T> Get<T>(string key);
}
