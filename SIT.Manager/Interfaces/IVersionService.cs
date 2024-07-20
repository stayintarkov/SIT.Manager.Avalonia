namespace SIT.Manager.Interfaces;

public interface IVersionService
{
    /// <summary>
    /// Gets the product version string for a given file path
    /// </summary>
    /// <param name="filePath">Path to the dll/exe to get the version from</param>
    /// <returns>Version string that is found; otherwise string.Empty</returns>
    string GetFileProductVersionString(string filePath);
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
