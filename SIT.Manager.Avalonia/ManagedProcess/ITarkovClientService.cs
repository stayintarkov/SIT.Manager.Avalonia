namespace SIT.Manager.Avalonia.ManagedProcess
{
    // Trust microsoft to not have name registration unlike every other DI library
    public interface ITarkovClientService : IManagedProcess
    {
        /// <summary>
        /// Clear just the EFT local cache.
        /// </summary>
        void ClearLocalCache();
    }
}
