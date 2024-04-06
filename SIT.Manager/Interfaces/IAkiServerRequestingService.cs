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
    //public Task<int> PingAsync(AkiServer akiServer, CancellationToken cancellationToken = default);
    public Task<AkiServer> GetAkiServerAsync(Uri serverAddresss, bool fetchInformation = true);
}
