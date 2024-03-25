using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIT.Manager.Avalonia.Models;
public class AkiProfile(string profileID)
{
    public string ProfileID { get; } = profileID;
}
