using SIT.Manager.Native.Linux;

namespace SIT.Manager.Native.Linux.Managers;

public class Dgvoodoo2Manager() : DllManager("dgvodoo2",
    [
        "d3dimm",
        "ddraw",
        "glide",
        "glide2x",
        "glide3x",
    ],
    "dgvoodoo2",
    "https://api.github.com/repos/lutris/dgvoodoo2/releases",
    ["dgVoodoo/dgVoodoo.conf"]); // TODO: Implement this, probs somewhere in the appdata folder of the prefix
