using SIT.Manager.Interfaces;
using SIT.Manager.Models;
using System;

namespace SIT.Manager.Services;

public class ActionNotificationService : IActionNotificationService
{
    private bool _isShowingNotification = false;

    public event EventHandler<ActionNotification>? ActionNotificationReceived;

    public void StartActionNotification()
    {
        if (_isShowingNotification)
        {
            return;
        }
        _isShowingNotification = true;

        ActionNotificationReceived?.Invoke(this, new ActionNotification(string.Empty, 0, true));
    }

    public void StopActionNotification()
    {
        if (!_isShowingNotification)
        {
            return;
        }
        _isShowingNotification = false;

        ActionNotificationReceived?.Invoke(this, new ActionNotification(string.Empty, 0, false));
    }

    public void UpdateActionNotification(ActionNotification notification)
    {
        if (!_isShowingNotification)
        {
            return;
        }
        ActionNotificationReceived?.Invoke(this, notification);
    }
}
