using CommunityToolkit.Mvvm.Messaging.Messages;
using System;

namespace SIT.Manager.Models.Play;

public class DeleteServerMessage(Uri serverUri) : ValueChangedMessage<Uri>(serverUri)
{
}
