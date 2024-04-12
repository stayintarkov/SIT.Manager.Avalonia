using SIT.Manager.Models.Github;
using System.Collections.Generic;

namespace SIT.Manager.Models.Installation;

public class SitInstallVersion
{
    public bool IsAvailable { get; set; } = false;
    public bool DowngradeRequired { get; set; } = false;
    public string EftVersion { get; set; } = string.Empty;
    public string SitVersion { get; set; } = string.Empty;
    public bool IsDeveloperVersion => Release.Prerelease;
    public GithubRelease Release { get; set; } = new();
    public Dictionary<string, string> DownloadMirrors { get; set; } = [];
}
