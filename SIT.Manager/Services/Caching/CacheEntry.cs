using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SIT.Manager.Services.Caching;
internal class CacheEntry
{
    private object _cacheValue;
    public string Key { get; private set; }
    public DateTime ExpiryDate { get; private set; }
    public DateTime LastAccess { get; private set; } = default;
    public DateTime LastModified { get; private set; } = DateTime.Now;
    public object Value
    {
        get
        {
            LastAccess = DateTime.UtcNow;
            return _cacheValue;
        }
        set
        {
            _cacheValue = value;
            LastAccess = DateTime.UtcNow;
            LastModified = DateTime.UtcNow;
        }
    }

    [JsonConstructor]
    public CacheEntry(string key, object value, DateTime expiryDate)
    {
        this.Key = key;
        this._cacheValue = value;
        this.ExpiryDate = expiryDate;
    }

    public T GetValue<T>()
    {
        object val = Value;

        Type type = typeof(T);
        Type? underlying = Nullable.GetUnderlyingType(type);
        if(type.IsPrimitive)
        {
            if(underlying == null)
            {
                return (T) Convert.ChangeType(val, type);
            }
            else
            {
                return (T) Convert.ChangeType(val, underlying);
            }
        }

        return (T) val;
    }
}
