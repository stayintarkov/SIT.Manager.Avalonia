using SIT.Manager.Models.Config;
using System;

namespace SIT.Manager.Interfaces;

public interface IManagerConfigService
{
    public ManagerConfig Config { get; }
}
