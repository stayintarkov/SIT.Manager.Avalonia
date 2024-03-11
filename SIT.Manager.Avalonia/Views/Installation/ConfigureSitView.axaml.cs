using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using SIT.Manager.Avalonia.ViewModels.Installation;

namespace SIT.Manager.Avalonia.Views.Installation;

public partial class ConfigureSitView : ReactiveUserControl<ConfigureSitViewModel>
{
    public ConfigureSitView()
    {
        InitializeComponent();
        this.DataContext = App.Current.Services.GetService<ConfigureSitViewModel>();
        this.WhenActivated(disposables => { /* Handle view activation etc. */ });
    }
}
