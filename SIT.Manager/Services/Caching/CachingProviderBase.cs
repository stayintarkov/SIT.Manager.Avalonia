using Avalonia.Controls.ApplicationLifetimes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SIT.Manager.Services.Caching;
internal abstract class CachingProviderBase : ICachingProvider
{
    protected readonly ConcurrentDictionary<string, CacheEntry> CacheMap = new();
    private readonly Timer _landlord;
    public event EventHandler<EvictedEventArgs>? Evicted;

    protected CachingProviderBase()
    {
        _landlord = new Timer(EvictTenants, CacheMap, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(0.5));
    }

    public virtual void EvictTenants(object? state = null)
    {
        lock (CacheMap)
        {
            foreach (CacheEntry entry in CacheMap.Values)
            {
                if (!entry.Expired) continue;
                TryRemove(entry.Key);
            }   
        }
    }

    public virtual void Clear(string prefix = "")
    {
        IEnumerable<CacheEntry> entriesToRemove = string.IsNullOrWhiteSpace(prefix)
            ? CacheMap.Values
            : CacheMap.Values.Where(x => x.Key.StartsWith(prefix));

        foreach (CacheEntry entry in entriesToRemove)
        {
            CacheMap.TryRemove(entry.Key, out _);
        }
    }
    public virtual bool Exists(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
        return CacheMap.TryGetValue(key, out CacheEntry? entry) && !entry.Expired;
    }
    public virtual IEnumerable<string> GetAllKeys(string prefix)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        return CacheMap.Values
            .Where(x => x.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && !x.Expired)
            .Select(x => x.Key).ToList();
    }
    public virtual int GetCount(string prefix = "")
    {
        IEnumerable<CacheEntry> cacheItems = CacheMap.Values.Where(x => !x.Expired);
        if (!string.IsNullOrWhiteSpace(prefix))
        {
            cacheItems = cacheItems.Where(x => x.Key.StartsWith(prefix));
        }

        return cacheItems.Count();
    }
    public virtual bool TryRemove(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
        bool keyRemoved = CacheMap.TryRemove(key, out _);
        if(keyRemoved) OnEvictedTenant(new EvictedEventArgs(key));
        return keyRemoved;
    }
    public virtual int TryRemoveByPrefix(string prefix)
    {
        var keysToRemove =
            CacheMap.Keys.Where(x => x.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        return keysToRemove.Count(TryRemove);
    }
    
    public virtual async Task<CacheValue<T>> GetOrComputeAsync<T>(string key, Func<string, Task<T>> computer, TimeSpan? expiryTime = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
        
        if (TryGet(key, out CacheValue<T> valOut))
            return valOut;

        T computedValue = await computer(key);
        _ = TryAdd(key, computedValue, expiryTime);

        return Get<T>(key);
    }
    public virtual bool TryGet<T>(string key, out CacheValue<T> cacheValue)
    {
        cacheValue = Get<T>(key);
        return cacheValue != CacheValue<T>.NoValue && cacheValue != CacheValue<T>.Null;
    }

    public virtual void OnEvictedTenant(EvictedEventArgs e) => Evicted?.Invoke(this, e);
    public virtual bool TryAdd<T>(string key, T value, TimeSpan? expiryTime = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
        ArgumentNullException.ThrowIfNull(value, nameof(value));

        DateTime expiryDate = DateTime.UtcNow + (expiryTime ?? TimeSpan.FromMinutes(15));
        CacheEntry cacheEntry = new(key, value, expiryDate);
        if (cacheEntry.Expired) return false;

        //TODO: I can foresee this causing an issue in the future. I need to decide what to do here
        if (CacheMap.TryGetValue(key, out CacheEntry? existingEntry) && !existingEntry.Expired) return false;
        CacheMap.AddOrUpdate(cacheEntry.Key, cacheEntry, (_, _) => cacheEntry);

        return true;
    }
    public abstract CacheValue<T> Get<T>(string key);

    protected bool TryGetCacheEntry(string key, [MaybeNullWhen(false)] out CacheEntry cacheEntry)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));

        if (!CacheMap.TryGetValue(key, out cacheEntry)) return false;
        if (!cacheEntry.Expired) return true;

        TryRemove(cacheEntry.Key);
        return false;
    }
}
