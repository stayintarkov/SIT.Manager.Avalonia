using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.Theme.Controls;
using SIT.Manager.ViewModels.Settings;

namespace SIT.Manager.Views.Settings;

public partial class LauncherView : ActivatableUserControl
{
    public LauncherView()
    {
        InitializeComponent();
        DataContext = App.Current.Services.GetService<LauncherViewModel>();
    }
}
