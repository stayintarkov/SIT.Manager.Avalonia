using SIT.Manager.Avalonia.Models;
using System;

namespace SIT.Manager.Avalonia.Interfaces
{
    public interface IActionNotificationService
    {
        event EventHandler<ActionNotification>? ActionNotificationReceived;

        void StartActionNotification();
        void StopActionNotification();
        void UpdateActionNotification(ActionNotification notification);
    }
}
