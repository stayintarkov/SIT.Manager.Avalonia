using SIT.Manager.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SIT.Manager.Services.Caching;
public class CachingService : ICachingService
{
    public ICachingProvider InMemory { get; } = new InMemoryCachingService();
    //public readonly ICachingService OnDisk = new OnDiskCachingService();
}
