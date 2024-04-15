using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIT.Manager.Services.Caching;
internal class CacheEntry(string key, object value, DateTime expiaryDate)
{
    private object _cacheValue = value;
    internal string Key { get; private set; } = key;
    internal DateTime ExpiraryDate { get; private set; } = expiaryDate;
    internal DateTime LastAccess { get; private set; } = default;
    internal DateTime LastModified { get; private set; } = DateTime.Now;
    internal object Value
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
