using SIT.Manager.Models.Aki;
using System.Threading.Tasks;

namespace SIT.Manager.Interfaces.ManagedProcesses;

public interface ITarkovClientService : IManagedProcess
{
    /// <summary>
    /// Clear just the EFT local cache.
    /// </summary>
    void ClearLocalCache();
    Task ConnectToServer(AkiCharacter character);
    Task CreateCharacter(AkiServer server);
}
