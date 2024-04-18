using System.Collections.Generic;

namespace SIT.Manager.Linux.Managers;

public class DxvkManager() : DllManager("DXVK",
    [
        "dxgi",
        "d3d11",
        "d3d10core",
        "d3d9"
    ],
    "dxvk",
    "https://api.github.com/repos/lutris/dxvk/releases");
