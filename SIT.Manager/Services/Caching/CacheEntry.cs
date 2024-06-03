using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SIT.Manager.Services.Caching;

[method: JsonConstructor]
internal class CacheEntry(string key, object cacheValue, DateTime expiryDate)
{
    public string Key { get; } = key;
    public DateTime ExpiryDate { get; } = expiryDate;
    public bool Expired => ExpiryDate <= DateTime.UtcNow;
    //TODO: Implement these to have a use
    public DateTime LastAccess { get; private set; }
    public DateTime LastModified { get; private set; } = DateTime.UtcNow;
    public object Value
    {
        get
        {
            LastAccess = DateTime.UtcNow;
            return cacheValue;
        }
        set
        {
            cacheValue = value;
            LastAccess = DateTime.UtcNow;
            LastModified = DateTime.UtcNow;
        }
    }

    public T GetValue<T>()
    {
        object val = Value;

        Type type = typeof(T);
        Type? underlying = Nullable.GetUnderlyingType(type);
        
        if(type.IsPrimitive) return (T) Convert.ChangeType(val, underlying ?? type);
        if (val.GetType() != typeof(JsonElement)) return (T) val;

        JsonElement jElement = (JsonElement) val;
        return jElement.Deserialize<T>()!; //I hate using this but we *know* this will never be null
    }
}
