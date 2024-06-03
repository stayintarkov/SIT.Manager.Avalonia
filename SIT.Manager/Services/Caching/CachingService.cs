using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.Interfaces;
using System;

namespace SIT.Manager.Services.Caching;

public class CachingService(IServiceProvider provider) : ICachingService
{
    public ICachingProvider InMemory { get; } = ActivatorUtilities.CreateInstance<InMemoryCachingProvider>(provider);
    public ICachingProvider OnDisk { get; } = ActivatorUtilities.CreateInstance<OnDiskCachingProvider>(provider);
}
