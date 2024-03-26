using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.Avalonia.Controls;
using SIT.Manager.Avalonia.ViewModels.Installation;

namespace SIT.Manager.Avalonia.Views.Installation;

public partial class InstallView : ActivatableUserControl
{
    public InstallView()
    {
        InitializeComponent();
        this.DataContext = App.Current.Services.GetService<InstallViewModel>();
    }
}
