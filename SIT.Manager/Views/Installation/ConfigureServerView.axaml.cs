using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.Theme.Controls;
using SIT.Manager.ViewModels.Installation;

namespace SIT.Manager.Views.Installation;

public partial class ConfigureServerView : ActivatableUserControl
{
    public ConfigureServerView()
    {
        InitializeComponent();
        this.DataContext = App.Current.Services.GetService<ConfigureServerViewModel>();
    }
}
