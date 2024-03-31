namespace SIT.Manager.Interfaces;

public interface IVersionService
{
    /// <summary>
    /// Checks and returns the installed SPT-AKI version string
    /// </summary>
    /// <param name="path">The base SPT-AKI server path to check.</param>
    string GetSptAkiVersion(string path);
    /// <summary>
    /// Checks and returns the installed EFT version string
    /// </summary>
    /// <param name="path">The base EFT path to check.</param>
    string GetEFTVersion(string path);
    /// <summary>
    /// Checks and returns the installed SIT version string
    /// </summary>
    /// <param name="path">The base EFT path to check.</param>
    string GetSITVersion(string path);
    /// <summary>
    /// Checks and returns the installed SIT Mod version string
    /// </summary>
    /// <param name="path">The base SPT-AKI server path to check.</param>
    string GetSitModVersion(string path);
}
