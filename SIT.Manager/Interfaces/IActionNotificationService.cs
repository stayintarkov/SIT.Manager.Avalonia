using SIT.Manager.Models;
using System;

namespace SIT.Manager.Interfaces;

public interface IActionNotificationService
{
    event EventHandler<ActionNotification>? ActionNotificationReceived;

    void StartActionNotification();
    void StopActionNotification();
    void UpdateActionNotification(ActionNotification notification);
}
