using CommunityToolkit.Mvvm.Messaging.Messages;

namespace SIT.Manager.Models.Installation;

public class InstallProcessStateChangedMessage(InstallProcessState installProcessState) : ValueChangedMessage<InstallProcessState>(installProcessState)
{
}
