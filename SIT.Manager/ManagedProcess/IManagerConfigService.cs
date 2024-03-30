using SIT.Manager.Models;
using System;

namespace SIT.Manager.ManagedProcess;

public interface IManagerConfigService
{
    ManagerConfig Config { get; }

    void UpdateConfig(ManagerConfig config, bool ShouldSave = true, bool? SaveAccount = false);
    event EventHandler<ManagerConfig>? ConfigChanged;
}
