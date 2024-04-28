using SIT.Manager.Models.Aki;
using System.Threading.Tasks;

namespace SIT.Manager.Interfaces.ManagedProcesses;

public interface ITarkovClientService : IManagedProcess
{
    /// <summary>
    /// Clear just the EFT local cache.
    /// </summary>
    void ClearLocalCache();
    /// <summary>
    /// Connect to the SPT-AKI server and launch Escape from Tarkov
    /// </summary>
    /// <param name="character">The EFT character we are trying to login as</param>
    /// <returns>true if successfully logged in and launched otherwise false</returns>
    Task<bool> ConnectToServer(AkiServer server, AkiCharacter character);
    Task<AkiCharacter?> CreateCharacter(AkiServer server, string username, string password, bool rememberLogin);
    Task<AkiCharacter?> CreateCharacter(AkiServer server);
}
