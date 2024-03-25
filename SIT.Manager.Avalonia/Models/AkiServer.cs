using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIT.Manager.Avalonia.Models;
public class AkiServer(Uri address) : ObservableObject
{
    public Uri Address = address;
    public string Name { get; internal set; } = string.Empty;
    public int Players { get; internal set; } = 0;
    public int Ping { get; internal set; } = -1;
}
