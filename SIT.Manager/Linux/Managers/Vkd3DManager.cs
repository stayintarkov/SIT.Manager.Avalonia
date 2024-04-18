using System.Collections.Generic;

namespace SIT.Manager.Linux.Managers;

public class Vkd3DManager() : DllManager("VKD3D",
    [
        "d3d12",
        "d3d12core"
    ],
    "vkd3d",
    "https://api.github.com/repos/lutris/vkd3d/releases");
