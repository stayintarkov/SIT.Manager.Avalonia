using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SIT.Manager.Services.Caching;
public interface ICachingProvider
{
    event EventHandler<EvictedEventArgs>? Evicted;

    bool Add<T>(string key, T value, TimeSpan? expiryTime = null);
    void Clear(string prefix = "");
    bool Exists(string key);
    CacheValue<T> Get<T>(string key);
    IEnumerable<string> GetAllKeys(string prefix);
    int GetCount(string prefix = "");
    CacheValue<T> GetOrCompute<T>(string key, Func<string, T> computor, TimeSpan? expiryTime = null);
    Task<CacheValue<T>> GetOrComputeAsync<T>(string key, Func<string, Task<T>> computor, TimeSpan? expiryTime = null);
    bool Remove(string key);
    int RemoveByPrefix(string prefix);
    bool TryGet<T>(string key, out CacheValue<T> cacheValue);
}
