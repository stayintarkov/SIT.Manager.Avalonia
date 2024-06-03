using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SIT.Manager.Services.Caching;

public interface ICachingProvider
{
    event EventHandler<EvictedEventArgs>? Evicted;
    void EvictTenants(object? state);
    void Clear(string prefix = "");
    bool Exists(string key);
    IEnumerable<string> GetAllKeys(string prefix);
    int GetCount(string prefix = "");
    bool TryRemove(string key);
    int TryRemoveByPrefix(string prefix);
    Task<CacheValue<T>> GetOrComputeAsync<T>(string key, Func<string, Task<T>> computer, TimeSpan? expiryTime = null);
    bool TryGet<T>(string key, out CacheValue<T> cacheValue);
    void OnEvictedTenant(EvictedEventArgs e);
    bool TryAdd<T>(string key, T value, TimeSpan? expiryTime = null);
    CacheValue<T> Get<T>(string key);
}
