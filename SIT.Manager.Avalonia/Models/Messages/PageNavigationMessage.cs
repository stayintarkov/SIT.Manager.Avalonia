using CommunityToolkit.Mvvm.Messaging.Messages;

namespace SIT.Manager.Avalonia.Models.Messages;

public class PageNavigationMessage : ValueChangedMessage<PageNavigation>
{
    public PageNavigationMessage(PageNavigation pageNavigation) : base(pageNavigation) { }
}
