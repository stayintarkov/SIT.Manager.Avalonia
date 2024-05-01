namespace SIT.Manager.Models;

public class ActionNotification(string actionText, double progressPercentage, bool showActionPanel = true)
{
    public string ActionText { get; set; } = actionText;
    public double ProgressPercentage { get; set; } = progressPercentage;
    public bool ShowActionPanel { get; set; } = showActionPanel;
}
