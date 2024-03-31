using FluentAvalonia.UI.Controls;

namespace SIT.Manager.Models;

public record BarNotification(string Title, string Message, InfoBarSeverity Severity, int Delay);
