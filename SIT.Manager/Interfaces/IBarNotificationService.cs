using FluentAvalonia.UI.Controls;
using SIT.Manager.Models;
using System;

namespace SIT.Manager.Interfaces;

public interface IBarNotificationService
{
    event EventHandler<BarNotification>? BarNotificationReceived;

    /// <summary>
    /// Shows a notification over the main window
    /// </summary>
    /// <param name="title">Title of the message</param>
    /// <param name="message">The message to show</param>
    /// <param name="severity">The <see cref="InfoBarSeverity"/> to display</param>
    /// <param name="delay">The delay (in seconds) before removing the InfoBar</param>
    void Show(string title, string message, InfoBarSeverity severity = InfoBarSeverity.Informational, int delay = 5);
    void ShowError(string title, string message, int delay = 5);
    void ShowInformational(string title, string message, int delay = 5);
    void ShowSuccess(string title, string message, int delay = 5);
    void ShowWarning(string title, string message, int delay = 5);
}
