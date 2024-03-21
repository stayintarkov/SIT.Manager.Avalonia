using System.Threading.Tasks;

namespace SIT.Manager.Avalonia.Interfaces;

public interface IAppUpdaterService
{
    /// <summary>
    /// Check if there is an update available for SIT.Manager.Avalonia if the user allows checking for updates in ManagerConfig
    /// </summary>
    /// <returns>true if an update is available, otherwise false</returns>
    Task<bool> CheckForUpdate();
    /// <summary>
    /// Download, extract and then replace the current manager with the newsest version
    /// </summary>
    /// <returns></returns>
    Task Update();
}
