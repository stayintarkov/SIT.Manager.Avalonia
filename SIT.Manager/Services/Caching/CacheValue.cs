using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIT.Manager.Services.Caching;
public class CacheValue<T>(T? value, bool hasValue)
{
    public bool HasValue { get { return hasValue; } }
    public T? Value { get { return value; } }
    public static CacheValue<T> Null { get; } = new CacheValue<T>(default, true);
    public static CacheValue<T> NoValue { get; } = new CacheValue<T>(default, false);
    public override string ToString()
    {
        return Value?.ToString() ?? "null";
    }
}
