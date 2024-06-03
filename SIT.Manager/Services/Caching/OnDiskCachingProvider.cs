using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PeNet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Hashing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace SIT.Manager.Services.Caching;

internal class OnDiskCachingProvider : CachingProviderBase
{
    private const string CachePath = "Cache";
    private const string RestoreFileName = "fileCache.dat";
    private readonly ILogger<OnDiskCachingProvider> _logger;
    private readonly XxHash32 _hasher = new();
    private readonly DirectoryInfo _cacheDirectory;
    private string RestoreFilePath => Path.Combine(_cacheDirectory.FullName, RestoreFileName);

    public OnDiskCachingProvider(ILogger<OnDiskCachingProvider> logger)
    {
        _logger = logger;
        _cacheDirectory = new DirectoryInfo(CachePath);
        Evicted += (_, e) => RemoveCacheFile(e.Key);
        
        if (App.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            lifetime.ShutdownRequested += (o, e) => SaveKeysToFile();
        
        if (!File.Exists(RestoreFilePath)) return;

        try
        {
            CacheMap = JsonConvert.DeserializeObject<ConcurrentDictionary<string, CacheEntry>>(RestoreFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An exception occured while attempting to restore cache from file.");
        }
    }

    private void SaveKeysToFile()
    {
        try
        {
            _cacheDirectory.Create();
            using FileStream fs = File.OpenWrite(RestoreFilePath);
            JsonSerializer.Serialize(fs, CacheMap);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An exception occured while attempting to save cache to restore file.");
        }
    }

    private void RemoveCacheFile(string key)
    {
        string filename = HashKey(key);
        string filePath = Path.Combine(_cacheDirectory.FullName, filename);
        
        if(File.Exists(filePath)) File.Delete(filePath);
    }

    private string HashKey(string key)
    {
        //I would use Span<byte> but BitConverter doesn't have an overload for it
        byte[] hashBuffer = new byte[_hasher.HashLengthInBytes]; 
        _hasher.Append(Encoding.UTF8.GetBytes(key));
        _hasher.GetHashAndReset(hashBuffer);
        return BitConverter.ToString(hashBuffer);
    }
    
    public override bool TryAdd<T>(string key, T value, TimeSpan? expiryTime = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
        ArgumentNullException.ThrowIfNull(value, nameof(value));

        if (Exists(key)) return false;

        string filename = HashKey(key);
        string filePath = Path.Combine(_cacheDirectory.FullName, filename);
        
        _cacheDirectory.Create();
        using (FileStream fs = File.OpenWrite(filePath))
        {
            if (value is Stream inputStream)
            {
                if (inputStream.CanSeek)
                    inputStream.Seek(0, SeekOrigin.Begin);

                inputStream.CopyTo(fs);
            }
            else
            {
                Type? elementType = value.GetType().GetElementType();
                //ElementType indicates this is an array of bytes
                if (elementType == typeof(byte)) fs.Write(value as byte[]);
                else JsonSerializer.Serialize(fs, value);
            }
        }

        bool success = base.TryAdd(key, filePath, expiryTime ?? TimeSpan.FromMinutes(15));
        if (!success) File.Delete(filePath);
        SaveKeysToFile();
        return success;
    }

    public override CacheValue<T> Get<T>(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));

        if (!TryGetCacheEntry(key, out CacheEntry? cacheEntry)) return CacheValue<T>.NoValue;

        try
        {
            string filePath = cacheEntry.GetValue<string>();
            if (!File.Exists(filePath))
            {
                TryRemove(cacheEntry.Key);
                return CacheValue<T>.NoValue;
            }

            Type genericType = typeof(T);
            //Note: This stream should *not* be in a using, otherwise it'll dispose on return which is useless
            FileStream fs = File.OpenRead(filePath);
            if (genericType == typeof(Stream)) return new CacheValue<T>((T)(object) fs, true);

            Span<byte> fileBytes;
            try
            {
                fileBytes = new byte[fs.Length - fs.Position];
                _ = fs.Read(fileBytes);
            }
            finally
            {
                fs.Dispose();
            }
            
            return genericType == typeof(byte[]) ?
                new CacheValue<T>((T) (object) fileBytes.ToArray(), true) :
                new CacheValue<T>(JsonSerializer.Deserialize<T>(fileBytes), true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occured during cast or file opening.");
            return CacheValue<T>.NoValue;
        }
    }
}
