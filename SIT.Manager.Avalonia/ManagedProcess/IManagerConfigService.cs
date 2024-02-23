using SIT.Manager.Avalonia.Models;
using System;

namespace SIT.Manager.Avalonia.ManagedProcess
{
    public interface IManagerConfigService
    {
        ManagerConfig Config { get; }

        void UpdateConfig(ManagerConfig config, bool ShouldSave = true, bool SaveAccount = false);
        event EventHandler<ManagerConfig>? ConfigChanged;
    }
}
