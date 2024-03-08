using CommunityToolkit.Mvvm.Messaging.Messages;

namespace SIT.Manager.Avalonia.Models.Messages
{
    /// <summary>
    /// Message to inform the InstallPage whether to progress the stepper to the next value
    /// </summary>
    /// <param name="value">True goes forawrd a step; False goes back a step</param>
    public class InstallationProgressMessage(bool value) : ValueChangedMessage<bool>(value)
    {
    }
}
