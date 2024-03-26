using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.Avalonia.Controls;
using SIT.Manager.Avalonia.ViewModels.Installation;

namespace SIT.Manager.Avalonia.Views.Installation;

public partial class ConfigureSitView : ActivatableUserControl
{
    public ConfigureSitView()
    {
        InitializeComponent();
        this.DataContext = App.Current.Services.GetService<ConfigureSitViewModel>();
    }
}
