using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SIT.Manager.Services.Caching;
public class CachingService(IServiceProvider provider) : ICachingService
{
    private const string CACHE_PATH = "Cache";
    public ICachingProvider InMemory { get; } = ActivatorUtilities.CreateInstance<InMemoryCachingProvider>(provider, CACHE_PATH);
    public ICachingProvider OnDisk { get; } = ActivatorUtilities.CreateInstance<OnDiskCachingProvider>(provider, CACHE_PATH);
}
