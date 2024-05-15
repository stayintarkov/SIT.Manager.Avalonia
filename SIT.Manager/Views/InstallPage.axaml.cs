using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.Theme.Controls;
using SIT.Manager.ViewModels;

namespace SIT.Manager.Views;

public partial class InstallPage : ActivatableUserControl
{
    public InstallPage()
    {
        InitializeComponent();
        DataContext = App.Current.Services.GetService<InstallPageViewModel>();
    }
}
