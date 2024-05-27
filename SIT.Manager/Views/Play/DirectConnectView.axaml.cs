using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.Theme.Controls;
using SIT.Manager.ViewModels.Play;

namespace SIT.Manager.Views.Play;

public partial class DirectConnectView : ActivatableUserControl
{
    public DirectConnectView()
    {
        InitializeComponent();
        DataContext = App.Current.Services.GetService<DirectConnectViewModel>();
        if (DataContext is DirectConnectViewModel dataContext)
        {
            AddressBox.LostFocus += (o, e) => { if (!dataContext.ManagerConfig.LauncherSettings.HideIpAddress) AddressBox.RevealPassword = true; };
        }
    }
}
