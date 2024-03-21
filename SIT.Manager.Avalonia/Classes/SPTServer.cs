using SIT.Manager.Avalonia.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SIT.Manager.Avalonia.Classes;
public class SPTServer(Uri remoteEndpoint, TarkovRequestingService tarkovRequestingService)
{
    private readonly Uri _remoteEndpoint = remoteEndpoint;
    private readonly TarkovRequestingService _tarkovRequesting = tarkovRequestingService;

    public async Task<bool> PingAsync(CancellationToken cancellationToken = default)
    {
        using Stream postStream = await _tarkovRequesting.PostAsync(_remoteEndpoint, "/launcher/ping", string.Empty, cancellationToken);
        using MemoryStream ms = new();
        await postStream.CopyToAsync(ms, cancellationToken);
        string decodedResp = SimpleZlib.Decompress(ms.ToArray());
        return decodedResp.Equals("\"pong!\"");
    }
}
