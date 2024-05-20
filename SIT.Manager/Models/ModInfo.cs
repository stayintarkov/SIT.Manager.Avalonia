namespace SIT.Manager.Models;

public class ModInfo
{
    public string Name { get; set; } = string.Empty;
    public string ModVersion { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public bool IsRequired { get; set; } = false;
}
