using CommunityToolkit.Mvvm.Messaging.Messages;

namespace SIT.Manager.Models.Installation;

/// <summary>
/// Message to inform the InstallPage whether to progress the stepper to the next value
/// </summary>
/// <param name="value">True goes forawrd a step; False goes back a step</param>
public class ProgressInstallMessage(bool value) : ValueChangedMessage<bool>(value)
{
}
