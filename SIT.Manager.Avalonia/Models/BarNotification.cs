using FluentAvalonia.UI.Controls;

namespace SIT.Manager.Avalonia.Models
{
    public record BarNotification(string Title, string Message, InfoBarSeverity Severity, int Delay);
}
