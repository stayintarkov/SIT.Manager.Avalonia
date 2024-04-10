using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIT.Manager.Models.Aki;
public class AkiProfile(AkiServer parentServer, string profileID)
{
    public string ProfileID { get; internal set; } = profileID;
    public string Username { get; internal set; } = string.Empty;
    public string Password { get; internal set; } = string.Empty;
    public string Edition { get; internal set; } = string.Empty;
    public AkiServer ParentServer { get; } = parentServer;

}
