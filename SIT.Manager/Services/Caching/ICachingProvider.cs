using System;
using System.Collections.Generic;

namespace SIT.Manager.Services.Caching;
public interface ICachingProvider
{
    event EventHandler<EvictedEventArgs>? Evicted;

    bool Add<T>(string key, T value, TimeSpan? expiryTime = null);
    void Clear(string prefix = "");
    bool Exists(string key);
    object? Get(string key);
    CacheValue<T> Get<T>(string key);
    IEnumerable<string> GetAllKeys(string prefix);
    int GetCount(string prefix = "");
    bool Remove(string key);
    int RemoveByPrefix(string prefix);
}