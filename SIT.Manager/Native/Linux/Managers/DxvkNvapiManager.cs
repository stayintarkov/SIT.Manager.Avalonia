using SIT.Manager.Native.Linux;

namespace SIT.Manager.Native.Linux.Managers;

public class DxvkNvapiManager() : DllManager("DXVK-NVAPI",
    [
        "nvapi",
        "nvapi64",
        "nvml"
    ],
    "dxvk-nvapi",
    "https://api.github.com/repos/lutris/dxvk-nvapi/releases");
// TODO: dlss_dlls = ("nvngx", "_nvngx")
// I (jubiman) cannot test this as I have an AMD GPU.
// Please someone that is on Linux with an NVIDIA GPU implement and test this.
