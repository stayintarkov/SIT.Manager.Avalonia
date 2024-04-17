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
internal class OnDiskCachingProvider(string cachePath) : CachingProviderBase(cachePath)
{
    private const string RESTORE_FILE_NAME = "fileCache.dat";
    protected override string RestoreFileName => RESTORE_FILE_NAME;

    public override bool Add<T>(string key, T value, TimeSpan? expiryTime = null)
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
        bool success = _cacheMap.TryAdd(key, new CacheEntry(key, filePath, expiryDate));
        if (success)
        {
            return true;
        }
        else
            throw new Exception("Key didn't exist but couldn't add key to cache.");
    }

    public override CacheValue<T> Get<T>(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));

        if (!_cacheMap.TryGetValue(key, out CacheEntry? cacheEntry))
            return CacheValue<T>.NoValue;

        if (cacheEntry.ExpiryDate < DateTime.UtcNow)
        {
            RemoveExpiredKey(key);
            return CacheValue<T>.NoValue;
        }

        try
        {
            string filePath = cacheEntry.GetValue<string>();
            if (!File.Exists(filePath))
                return CacheValue<T>.Null;

            Type tType = typeof(T);
            FileStream fs = File.OpenRead(filePath);
            if (tType == typeof(FileStream))
            {
                return new CacheValue<T>((T) (object) fs, true);
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

            if (tType == typeof(string))
            {
                return new CacheValue<T>((T) (object) Encoding.UTF8.GetString(fileBytes), true);
            }
            return new CacheValue<T>(JsonSerializer.Deserialize<T>(fileBytes), true);
        }
        catch (Exception ex)
        {
            //TODO: log exception
            return CacheValue<T>.NoValue;
        }
    }
}
