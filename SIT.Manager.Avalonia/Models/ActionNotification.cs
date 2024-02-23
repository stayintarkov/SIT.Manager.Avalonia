namespace SIT.Manager.Avalonia.Models
{
    public class ActionNotification
    {
        public string ActionText { get; set; }
        public double ProgressPercentage { get; set; }
        public bool ShowActionPanel { get; set; }

        public ActionNotification(string actionText, double progressPercentage, bool showActionPanel = true) {
            ActionText = actionText;
            ProgressPercentage = progressPercentage;
            ShowActionPanel = showActionPanel;
        }
    }
}
