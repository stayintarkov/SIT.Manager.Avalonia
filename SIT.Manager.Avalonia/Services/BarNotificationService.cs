using FluentAvalonia.UI.Controls;
using SIT.Manager.Avalonia.Interfaces;
using SIT.Manager.Avalonia.Models;
using System;

namespace SIT.Manager.Avalonia.Services
{
    public class BarNotificationService : IBarNotificationService
    {
        public event EventHandler<BarNotification>? BarNotificationReceived;

        public BarNotificationService() { }

        public void Show(string title, string message, InfoBarSeverity severity = InfoBarSeverity.Informational, int delay = 5) {
            BarNotification newNotification = new(title, message, severity, delay);
            BarNotificationReceived?.Invoke(this, newNotification);
        }

        public void ShowError(string title, string message, int delay = 5) {
            Show(title, message, InfoBarSeverity.Error, delay);
        }

        public void ShowInformational(string title, string message, int delay = 5) {
            Show(title, message, InfoBarSeverity.Informational, delay);
        }

        public void ShowSuccess(string title, string message, int delay = 5) {
            Show(title, message, InfoBarSeverity.Success, delay);
        }

        public void ShowWarning(string title, string message, int delay = 5) {
            Show(title, message, InfoBarSeverity.Warning, delay);
        }
    }
}
