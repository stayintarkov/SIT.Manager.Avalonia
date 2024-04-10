using SIT.Manager.Models.Aki;
using SIT.Manager.Services;
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
    public Task<AkiServer> GetAkiServerAsync(Uri serverAddresss, bool fetchInformation = true, CancellationToken cancellationToken = default);
    public Task<List<AkiMiniProfile>> GetMiniProfilesAsync(AkiServer server, CancellationToken cancellationToken = default);
    public Task<(string, AkiLoginStatus)> LoginAsync(AkiCharacter character, CancellationToken cancellationToken = default);
    public Task<(string, AkiLoginStatus)> RegisterCharacterAsync(AkiCharacter character, CancellationToken cancellationToken = default);
    public Task<AkiServerInfo?> GetAkiServerInfoAsync(AkiServer server, CancellationToken cancellationToken = default);
}
