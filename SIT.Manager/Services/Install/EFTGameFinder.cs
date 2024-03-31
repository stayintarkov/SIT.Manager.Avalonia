using Microsoft.Win32;
using System.IO;

namespace SIT.Manager.Services.Install;

internal static class EFTGameFinder
{
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
        using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\EscapeFromTarkov"))
        {
            if (key != null)
            {
                return key.GetValue("DisplayIcon")?.ToString() ?? string.Empty;
            }
        }
        return string.Empty;
    }

    private static bool LC1A(string gfp)
    {
        FileInfo fiGFP = new(gfp);
        return (fiGFP.Exists && fiGFP.Length >= 647 * 1000);
    }

    private static bool LC2B(string gfp)
    {
        FileInfo fiBE = new(gfp.Replace(".exe", "_BE.exe"));
        return (fiBE.Exists && fiBE.Length >= 1024000);
    }

    private static bool LC3C(string gfp)
    {
        DirectoryInfo diBattlEye = new(gfp.Replace("EscapeFromTarkov.exe", "BattlEye"));
        return (diBattlEye.Exists);
    }

    public static string FindOfficialGamePath()
    {
        string gamePath = GetGameExePath();
        if (CheckGameIsValid(gamePath))
        {
            return gamePath;
        }
        return string.Empty;
    }
}
