using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SIT.Manager.Services;
public class CachingService
{
    private readonly ConcurrentDictionary<string, ICachedItem> _cache = new();
    private readonly Timer _landlord;
    public CachingService()
    {
        _landlord = new Timer(new TimerCallback(EvictTenents), _cache, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(3));
    }

    private void EvictTenents(object? state)
    {
        if (state == null)
            return;
        ConcurrentDictionary<string, ICachedItem> cache = (ConcurrentDictionary<string, ICachedItem>) state;
        foreach (KeyValuePair<string, ICachedItem> pair in cache)
        {
            if(pair.Value != null && pair.Value.Spoilt)
            {
                cache.Remove(pair.Key, out ICachedItem? _);
            }
        }
    }

    public TValue GetOrCompute<TValue>(string key, Func<string, TValue> computer, TimeSpan expiration)
    {
        if(_cache.TryGetValue(key, out ICachedItem? cachedItem))
        {
            if(!cachedItem.Spoilt)
                return (TValue)cachedItem;
        }

        object? newValue = computer(key) ?? throw new ArgumentNullException(nameof(computer), "A null value was generated.");
        if (_cache.TryAdd(key, new CachedItem(newValue, DateTime.UtcNow + expiration)))
        {
            return (TValue) newValue;
        }
        else
        {
            throw new Exception("Cache did not contain key but cannot add key to cache.");
        }
    }
}

public interface ICachedItem
{
    public object CachedObject { get; }
    public DateTime ExpirationDate { get; }
    public bool Spoilt { get; }

}

readonly struct CachedItem(object cachedObject, DateTime expirationDate) : ICachedItem
{
    private readonly object _cachedObject = cachedObject;
    private readonly DateTime _expirationDate = expirationDate;
    public readonly object CachedObject { get { return _cachedObject; } }
    public readonly DateTime ExpirationDate { get {  return _expirationDate; } }
    public readonly bool Spoilt => DateTime.UtcNow > ExpirationDate;
}
