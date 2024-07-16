using Microsoft.Win32;
using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Text;

namespace SIT.Manager.Services.Install;

internal static class EFTGameFinder
{
    private static byte[] subkeyBytes =
    [
        0x55, 0x32, 0x39, 0x6d, 0x64, 0x48, 0x64, 0x68, 0x63, 0x6d, 0x56, 0x63, 0x56, 0x32, 0x39, 0x33, 0x4e, 0x6a, 0x51, 0x7a, 0x4d, 0x6b, 0x35, 0x76, 0x5a, 0x47, 0x56, 0x63, 0x54, 0x57, 0x6c, 0x6a, 0x63, 0x6d, 0x39, 0x7a, 0x62, 0x32, 0x5a, 0x30, 0x58, 0x46, 0x64, 0x70, 0x62, 0x6d, 0x52, 0x76, 0x64, 0x33, 0x4e, 0x63, 0x51, 0x33, 0x56, 0x79, 0x63, 0x6d, 0x56, 0x75, 0x64, 0x46, 0x5a, 0x6c, 0x63, 0x6e, 0x4e, 0x70, 0x62, 0x32, 0x35, 0x63, 0x56, 0x57, 0x35, 0x70, 0x62, 0x6e, 0x4e, 0x30, 0x59, 0x57, 0x78, 0x73, 0x58, 0x45, 0x56, 0x7a, 0x59, 0x32, 0x46, 0x77, 0x5a, 0x55, 0x5a, 0x79, 0x62, 0x32, 0x31, 0x55, 0x59, 0x58, 0x4a, 0x72, 0x62, 0x33, 0x59, 0x3d
    ];
    private static bool CheckGameIsValid(string path)
    {
        bool validGame = false;
        try
        {
            if (!string.IsNullOrEmpty(path))
            {
                validGame = LC1A(path);
                validGame = LC2B(path) && validGame;
                validGame = LC3C(path) && validGame;
            }
        }
        catch { }
        return validGame;
    }

    private static string GetGameExePath()
    {
        using RegistryKey? key = Registry.LocalMachine.OpenSubKey(Encoding.UTF8.GetString(Convert.FromBase64String(Encoding.UTF8.GetString(subkeyBytes))));
        if (key != null)
        {
            return key.GetValue("DisplayIcon")?.ToString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static bool LC1A(string gfp)
    {
        FileInfo fiGFP = new(gfp);
        return fiGFP is { Exists: true, Length: >= 647 * 1000 };
    }

    private static bool LC2B(string gfp)
    {
        FileInfo fiBE = new(gfp.Replace(".exe", "_BE.exe"));
        return fiBE is { Exists: true, Length: >= 1024000 };
    }

    private static bool LC3C(string gfp)
    {
        DirectoryInfo diBattlEye = new(gfp.Replace("EscapeFromTarkov.exe", "BattlEye"));
        return diBattlEye.Exists;
    }

    public static string FindOfficialGamePath()
    {
        string gamePath = GetGameExePath();
        return CheckGameIsValid(gamePath) ? gamePath : string.Empty;
    }
}
