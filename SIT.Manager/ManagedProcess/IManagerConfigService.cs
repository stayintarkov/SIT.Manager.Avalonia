using SIT.Manager.Models;
using System;

namespace SIT.Manager.ManagedProcess;

public interface IManagerConfigService
{
    ManagerConfig Config { get; }

    void UpdateConfig(ManagerConfig config, bool shouldSave = true, bool? saveAccount = false);
    event EventHandler<ManagerConfig>? ConfigChanged;
}
