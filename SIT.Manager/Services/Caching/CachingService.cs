using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.Interfaces;
using System;

namespace SIT.Manager.Services.Caching;

public class CachingService(IServiceProvider provider) : ICachingService
{
    private const string CACHE_PATH = "Cache";
    public ICachingProvider InMemory { get; } = ActivatorUtilities.CreateInstance<InMemoryCachingProvider>(provider, CACHE_PATH);
    public ICachingProvider OnDisk { get; } = ActivatorUtilities.CreateInstance<OnDiskCachingProvider>(provider, CACHE_PATH);
}
