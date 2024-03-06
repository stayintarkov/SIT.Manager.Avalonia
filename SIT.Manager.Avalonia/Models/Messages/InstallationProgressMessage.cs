using CommunityToolkit.Mvvm.Messaging.Messages;

namespace SIT.Manager.Avalonia.Models.Messages
{
    public class InstallationProgressMessage(bool value) : ValueChangedMessage<bool>(value)
    {
    }
}
