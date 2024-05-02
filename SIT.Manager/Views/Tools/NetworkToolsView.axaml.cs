using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.ViewModels.Tools;

namespace SIT.Manager.Views.Tools;
public partial class NetworkToolsView : UserControl
{
    public NetworkToolsView()
    {
        InitializeComponent();
        this.DataContext = App.Current.Services.GetService<NetworkToolsViewModel>();
    }
}
