using CommunityToolkit.Mvvm.Messaging.Messages;

namespace SIT.Manager.Models.Messages;

public class PageNavigationMessage(PageNavigation pageNavigation) : ValueChangedMessage<PageNavigation>(pageNavigation)
{
}
