using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.Theme.Controls;
using SIT.Manager.ViewModels.Tools;

namespace SIT.Manager.Views.Tools;

public partial class NetworkToolsView : ActivatableUserControl
{
    public NetworkToolsView()
    {
        InitializeComponent();
        DataContext = App.Current.Services.GetRequiredService<NetworkToolsViewModel>();
    }
}
