using Avalonia.Controls.ApplicationLifetimes;
using PeNet;
using SIT.Manager.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SIT.Manager.Services.Caching;
internal class OnDiskCachingProvider : ICachingProvider
{
    private const string RESTORE_FILE_NAME = "fileCache.dat";
    private readonly ConcurrentDictionary<string, CacheEntry> _cacheKeysMap;
    private readonly DirectoryInfo _cachePath;


    public event EventHandler<EvictedEventArgs>? Evicted;

    public OnDiskCachingProvider(string cachePath)
    {
        _cachePath = new(cachePath);
        _cachePath.Create();

        string restoreFilePath = Path.Combine(_cachePath.FullName, RESTORE_FILE_NAME);
        if (File.Exists(restoreFilePath))
        {
            _cacheKeysMap = JsonSerializer.Deserialize<ConcurrentDictionary<string, CacheEntry>>(File.ReadAllText(restoreFilePath)) ?? new();
        }
        else
        {
            _cacheKeysMap = new();
        }

        if (App.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            lifetime.ShutdownRequested += (sender, e) =>
            {
                SaveKeysToFile();
            };
        }
    }

    private void SaveKeysToFile()
    {
        if (_cacheKeysMap.IsEmpty)
            return;

        string keyDataPath = Path.Combine(_cachePath.FullName, RESTORE_FILE_NAME);
        File.WriteAllText(keyDataPath, JsonSerializer.Serialize(_cacheKeysMap));
    }

    public bool Add<T>(string key, T value, TimeSpan? expiryTime = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
        ArgumentNullException.ThrowIfNull(value, nameof(value));

        if (Exists(key))
            return false;

        string filename = MD5.HashData(Encoding.UTF8.GetBytes(key)).ToHexString();
        string filePath = Path.Combine(_cachePath.FullName, filename);
        using FileStream fs = File.OpenWrite(filePath);
        if (value is Stream inputStream)
        {
            if (inputStream.CanSeek)
                inputStream.Seek(0, SeekOrigin.Begin);

            inputStream.CopyTo(fs);
        }
        else
        {
            byte[] buffer;
            if (typeof(T) == typeof(byte[]))
            {
                buffer = value as byte[] ?? [];
            }
            else
            {
                string serializedData = JsonSerializer.Serialize(value);
                buffer = Encoding.UTF8.GetBytes(serializedData);
            }

            fs.Write(buffer, 0, buffer.Length);
        }

        DateTime expiryDate = DateTime.UtcNow + (expiryTime ?? TimeSpan.FromMinutes(15));
        bool success = _cacheKeysMap.TryAdd(key, new CacheEntry(key, filePath, expiryDate));
        if (success)
        {
            return true;
        }
        else
            throw new Exception("Key didn't exist but couldn't add key to cache.");
    }

    public void Clear(string prefix = "")
    {
        IEnumerable<CacheEntry> entriesToRemove = string.IsNullOrWhiteSpace(prefix) ? _cacheKeysMap.Values : _cacheKeysMap.Values.Where(x => x.Key.StartsWith(prefix));

        foreach (CacheEntry entry in entriesToRemove)
        {
            string filePath = entry.Value as string ?? string.Empty;
            if (File.Exists(filePath))
                File.Delete(filePath);
            _cacheKeysMap.Remove(entry.Key, out _);
        }
    }

    public bool Exists(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
        return _cacheKeysMap.TryGetValue(key, out CacheEntry? entry) && entry.ExpiryDate > DateTime.UtcNow;
    }

    internal void RemoveExpiredKey(string key)
    {
        if (_cacheKeysMap.TryRemove(key, out _))
        {
            Evicted?.Invoke(this, new EvictedEventArgs(key));
        }
    }

    public CacheValue<T> Get<T>(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));

        if(!_cacheKeysMap.TryGetValue(key, out CacheEntry? cacheEntry))
            return CacheValue<T>.NoValue;

        if(cacheEntry.ExpiryDate < DateTime.UtcNow)
        {
            RemoveExpiredKey(key);
            return CacheValue<T>.NoValue;
        }

        try
        {
            string filePath = cacheEntry.GetValue<string>();
            if(!File.Exists(filePath))
                return CacheValue<T>.Null;

            Type tType = typeof(T);
            FileStream fs = File.OpenRead(filePath);
            if (tType == typeof(FileStream))
            {
                return new CacheValue<T>((T)(object)fs, true);
            }

            byte[] fileBytes;
            MemoryStream ms = new MemoryStream();
            try
            {
                byte[] buffer = new byte[1024 * 8];
                int bytesRead;
                while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, bytesRead);
                }

                fileBytes = ms.ToArray();
                if (tType == typeof(byte[]))
                {
                    return new CacheValue<T>((T) (object) fileBytes, true);
                }
            }
            catch (Exception ex)
            {
                ms.Dispose();
                //TODO: logging
                return CacheValue<T>.Null;
            }

            if(tType == typeof(string))
            {
                return new CacheValue<T>((T)(object)Encoding.UTF8.GetString(fileBytes), true);
            }
            return new CacheValue<T>(JsonSerializer.Deserialize<T>(fileBytes), true);
        }
        catch (Exception ex)
        {
            //TODO: log exception
            return CacheValue<T>.NoValue;
        }
    }

    public IEnumerable<string> GetAllKeys(string prefix)
    {
        return _cacheKeysMap.Values
            .Where(x => x.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && x.ExpiryDate > DateTime.UtcNow)
            .Select(x => x.Key).ToList();
    }

    public int GetCount(string prefix = "")
    {
        IEnumerable<CacheEntry> cacheItems = cacheItems = _cacheKeysMap.Values.Where(x => x.ExpiryDate > DateTime.UtcNow);
        if (!string.IsNullOrWhiteSpace(prefix))
        {
            cacheItems = cacheItems.Where(x => x.Key.StartsWith(prefix));
        }

        return cacheItems.Count();
    }

    public CacheValue<T> GetOrCompute<T>(string key, Func<string, T> computor, TimeSpan? expiaryTime = null)
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

    public bool Remove(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
        return _cacheKeysMap.TryRemove(key, out _);
    }

    public int RemoveByPrefix(string prefix)
    {
        var keysToRemove = _cacheKeysMap.Keys.Where(x => x.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
        int removed = 0;
        foreach (var key in keysToRemove)
        {
            if (Remove(key))
                removed++;
        }
        return removed;
    }

    public bool TryGet<T>(string key, out CacheValue<T> cacheValue)
    {
        cacheValue = Get<T>(key);
        if (cacheValue == CacheValue<T>.NoValue || cacheValue == CacheValue<T>.Null)
            return false;
        return true;
    }
}
