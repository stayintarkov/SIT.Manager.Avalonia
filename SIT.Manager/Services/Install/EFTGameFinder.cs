using Microsoft.Win32;
using System;
using System.IO;
using System.Text;

namespace SIT.Manager.Services.Install;

internal static class EFTGameFinder
{
    private static byte[] subkeyBytes =
    [
        0x51, 0x43, 0x4A, 0x54, 0x62, 0x32, 0x5A, 0x30, 0x64, 0x32, 0x46, 0x79, 0x5A, 0x56, 0x78, 0x58, 0x62, 0x33,
        0x63, 0x32, 0x4E, 0x44, 0x4D, 0x79, 0x54, 0x6D, 0x39, 0x6B, 0x5A, 0x56, 0x78, 0x4E, 0x61, 0x57, 0x4E, 0x79,
        0x62, 0x33, 0x4E, 0x76, 0x5A, 0x6E, 0x52, 0x63, 0x56, 0x32, 0x6C, 0x75, 0x5A, 0x47, 0x39, 0x33, 0x63, 0x31,
        0x78, 0x44, 0x64, 0x58, 0x4A, 0x79, 0x5A, 0x57, 0x35, 0x30, 0x56, 0x6D, 0x56, 0x79, 0x63, 0x32, 0x6C, 0x76,
        0x62, 0x6C, 0x78, 0x56, 0x62, 0x6D, 0x6C, 0x75, 0x63, 0x33, 0x52, 0x68, 0x62, 0x47, 0x78, 0x63, 0x52, 0x58,
        0x4E, 0x6A, 0x59, 0x58, 0x42, 0x6C, 0x52, 0x6E, 0x4A, 0x76, 0x62, 0x56, 0x52, 0x68, 0x63, 0x6D, 0x74, 0x76,
        0x64, 0x69, 0x49, 0x3D
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
