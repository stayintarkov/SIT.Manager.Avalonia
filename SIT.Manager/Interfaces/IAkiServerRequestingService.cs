using SIT.Manager.Models.Aki;
using SIT.Manager.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SIT.Manager.Interfaces;
public interface IAkiServerRequestingService
{
    public Task<int> GetPingAsync(AkiServer akiServer, CancellationToken cancellationToken = default);
    public Task<AkiServer> GetAkiServerAsync(Uri serverAddress, bool fetchInformation = true, CancellationToken cancellationToken = default);
    public Task<List<AkiMiniProfile>> GetMiniProfilesAsync(AkiServer server, CancellationToken cancellationToken = default);
    public Task<(string, AkiLoginStatus)> LoginAsync(AkiServer server, AkiCharacter character, CancellationToken cancellationToken = default);
    public Task<(string, AkiLoginStatus)> RegisterCharacterAsync(AkiServer server, AkiCharacter character, CancellationToken cancellationToken = default);
    public Task<AkiServerInfo?> GetAkiServerInfoAsync(AkiServer server, CancellationToken cancellationToken = default);
    public Task<MemoryStream> GetAkiServerImage(AkiServer server, string assetPath, CancellationToken cancellationToken = default);
}
