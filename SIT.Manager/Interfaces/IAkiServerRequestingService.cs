using SIT.Manager.Models.Aki;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SIT.Manager.Interfaces;
public interface IAkiServerRequestingService
{
    public Task<int> GetPingAsync(AkiServer akiServer, CancellationToken cancellationToken = default);
    public Task<AkiServer> GetAkiServerAsync(Uri serverAddresss, bool fetchInformation = true);
    public Task<AkiServer> GetAkiServerAsync(AkiServer server, bool fetchInformation = true);
    public Task<List<AkiMiniProfile>> GetMiniProfilesAsync(AkiServer server, CancellationToken cancellationToken = default);
    public Task<string> LoginAsync(AkiCharacter character);
    public Task<string> RegisterCharacterAsync(AkiCharacter character);
}
