using SIT.Manager.Services.Caching;

namespace SIT.Manager.Interfaces;

public interface ICachingService
{
    public ICachingProvider InMemory { get; }
    public ICachingProvider OnDisk { get; }
}
