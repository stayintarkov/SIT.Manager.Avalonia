using CommunityToolkit.Mvvm.Messaging.Messages;

namespace SIT.Manager.Avalonia.Models.Installation;

/// <summary>
/// Message to inform whether there is an install running, so we can disable things like navigation to prevent users
/// from screwing up the install process
/// </summary>
/// <param name="value"></param>
public class InstallationRunningMessage(bool value) : ValueChangedMessage<bool>(value)
{
}
