using SIT.Manager.Avalonia.Interfaces;
using SIT.Manager.Avalonia.ManagedProcess;
using SIT.Manager.Avalonia.Models;
using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace SIT.Manager.Avalonia.Services;

public class ZlibService : IZlibService
{
    private Assembly? _zlibAssembly;
    private MethodInfo? compressToBytesMethodInfo;
    private MethodInfo? decompressMethodInfo;

    public ZlibService(IManagerConfigService configService)
    {
        if (string.IsNullOrEmpty(configService.Config.InstallPath))
            configService.ConfigChanged += LoadZlibFromConfig;
        else
            LoadZlibFromConfig(null, configService.Config);
    }

    public byte[] CompressToBytes(string data, ZlibCompression compressionProfile, Encoding? encoding = null)
        => (byte[]?) compressToBytesMethodInfo?.Invoke(null, [data, compressionProfile, encoding ?? Encoding.UTF8]) ?? [];

    public string Decompress(byte[] data, Encoding? encoding = null)
        => (string?) decompressMethodInfo?.Invoke(null, [data, encoding ?? Encoding.UTF8]) ?? string.Empty;

    //TODO: Add logging to this method
    private void LoadZlibFromConfig(object? sender, ManagerConfig e)
    {
        if (!string.IsNullOrEmpty(e.InstallPath))
        {
            string assemblyPath = Path.Combine(e.InstallPath, "EscapeFromTarkov_Data", "Managed", "bsg.componentace.compression.libs.zlib.dll");
            if (!File.Exists(assemblyPath))
                return;

            _zlibAssembly = Assembly.LoadFrom(assemblyPath);
            Type? SimpleZlib = _zlibAssembly?.GetType("ComponentAce.Compression.Libs.zlib.SimpleZlib");
            if (SimpleZlib != null)
            {
                compressToBytesMethodInfo = SimpleZlib?.GetMethod("CompressToBytes", [typeof(string), typeof(int), typeof(Encoding)]);
                decompressMethodInfo = SimpleZlib?.GetMethod("Decompress", [typeof(byte[]), typeof(Encoding)]);

                if (sender is IManagerConfigService configService && compressToBytesMethodInfo != null && decompressMethodInfo != null)
                    configService.ConfigChanged -= LoadZlibFromConfig;
            }
        }
    }
}
