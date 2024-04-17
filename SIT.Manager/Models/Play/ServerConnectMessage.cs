using CommunityToolkit.Mvvm.Messaging.Messages;
using SIT.Manager.Models.Aki;

namespace SIT.Manager.Models.Play;

public class ServerConnectMessage(AkiServer server) : ValueChangedMessage<AkiServer>(server)
{
}
